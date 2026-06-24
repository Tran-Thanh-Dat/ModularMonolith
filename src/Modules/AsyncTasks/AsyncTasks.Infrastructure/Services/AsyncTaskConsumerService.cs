using System.Text.Json;
using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Errors;
using AsyncTasks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AsyncTasks.Infrastructure.Services;

public sealed class AsyncTaskConsumerService : IAsyncTaskConsumerService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly AsyncTasksUnitOfWork _unitOfWork;
    private readonly IAsyncTaskService _taskService;
    private readonly IAsyncTaskProcessorRegistry _processorRegistry;
    private readonly ILogger<AsyncTaskConsumerService> _logger;

    public AsyncTaskConsumerService(
        AsyncTasksUnitOfWork unitOfWork,
        IAsyncTaskService taskService,
        IAsyncTaskProcessorRegistry processorRegistry,
        ILogger<AsyncTaskConsumerService> logger)
    {
        _unitOfWork = unitOfWork;
        _taskService = taskService;
        _processorRegistry = processorRegistry;
        _logger = logger;
    }

    public async Task ProcessMessageAsync(
        ProcessAsyncTaskMessage message,
        string consumerName,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Repository<Domain.Entities.AsyncTask, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == message.AsyncTaskId && !t.IsDeleted, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Async task {AsyncTaskId} not found for message {MessageId}", message.AsyncTaskId, message.MessageId);
            return;
        }

        if (task.Status is AsyncTaskStatuses.Completed or AsyncTaskStatuses.Cancelled)
        {
            _logger.LogInformation("Skipping message {MessageId} — task {TaskNo} already {Status}", message.MessageId, task.TaskNo, task.Status);
            return;
        }

        if (task.Status == AsyncTaskStatuses.Failed)
        {
            _logger.LogInformation("Skipping message {MessageId} — task {TaskNo} already failed", message.MessageId, task.TaskNo);
            return;
        }

        await _taskService.MarkProcessingAsync(message.AsyncTaskId, consumerName, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        task = await _unitOfWork.Repository<Domain.Entities.AsyncTask, Guid>()
            .QueryReadOnly()
            .FirstAsync(t => t.Id == message.AsyncTaskId, cancellationToken);

        if (task.Status == AsyncTaskStatuses.Cancelled)
        {
            return;
        }

        IAsyncTaskProcessor processor;
        try
        {
            processor = _processorRegistry.GetProcessor(message.TaskType);
        }
        catch (InvalidOperationException)
        {
            await _taskService.MarkFailedAsync(
                message.AsyncTaskId,
                AsyncTaskErrors.ProcessorNotFound,
                $"No processor registered for task type '{message.TaskType}'.",
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return;
        }

        var context = new AsyncTaskProcessingContext
        {
            AsyncTaskId = message.AsyncTaskId,
            TaskNo = message.TaskNo,
            TaskType = message.TaskType,
            Payload = message.Payload,
            TenantId = message.TenantId,
            OrganizationId = message.OrganizationId,
            RequestedByUserId = message.RequestedByUserId,
            CorrelationId = message.CorrelationId,
            MessageId = message.MessageId
        };

        var result = await processor.ProcessAsync(context, cancellationToken);
        await _taskService.MarkCompletedAsync(message.AsyncTaskId, result, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task HandleFaultAsync(
        ProcessAsyncTaskMessage message,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        var task = await _unitOfWork.Repository<Domain.Entities.AsyncTask, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == message.AsyncTaskId && !t.IsDeleted, cancellationToken);

        if (task is null)
        {
            _logger.LogWarning("Async task {AsyncTaskId} not found for fault message {MessageId}", message.AsyncTaskId, message.MessageId);
            return;
        }

        if (task.Status is AsyncTaskStatuses.Completed or AsyncTaskStatuses.Cancelled or AsyncTaskStatuses.Failed)
        {
            _logger.LogInformation(
                "Skipping fault for task {TaskNo} — already in terminal status {Status}",
                task.TaskNo,
                task.Status);
            return;
        }

        await _taskService.MarkFailedAsync(
            message.AsyncTaskId,
            AsyncTaskErrors.ProcessingFailed,
            errorMessage,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
