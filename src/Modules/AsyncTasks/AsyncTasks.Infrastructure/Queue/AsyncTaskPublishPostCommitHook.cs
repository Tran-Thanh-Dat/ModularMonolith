using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Domain.Errors;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using Microsoft.Extensions.Logging;

namespace AsyncTasks.Infrastructure.Queue;

public sealed class AsyncTaskPublishPostCommitHook : IPostCommitHook
{
    private readonly IAsyncTaskPublishBuffer _buffer;
    private readonly IAsyncTaskQueuePublisher _publisher;
    private readonly IAsyncTaskService _taskService;
    private readonly AsyncTasks.Infrastructure.Persistence.AsyncTasksUnitOfWork _unitOfWork;
    private readonly ILogger<AsyncTaskPublishPostCommitHook> _logger;

    public AsyncTaskPublishPostCommitHook(
        IAsyncTaskPublishBuffer buffer,
        IAsyncTaskQueuePublisher publisher,
        IAsyncTaskService taskService,
        AsyncTasks.Infrastructure.Persistence.AsyncTasksUnitOfWork unitOfWork,
        ILogger<AsyncTaskPublishPostCommitHook> logger)
    {
        _buffer = buffer;
        _publisher = publisher;
        _taskService = taskService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task OnCommittedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var message in _buffer.Drain())
        {
            try
            {
                await _publisher.PublishAsync(message, cancellationToken);
                await _taskService.MarkQueuedAsync(message.AsyncTaskId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to publish async task message {MessageId} for task {AsyncTaskId}",
                    message.MessageId,
                    message.AsyncTaskId);

                await _taskService.MarkFailedAsync(
                    message.AsyncTaskId,
                    AsyncTaskErrors.PublishFailed,
                    "Failed to publish message to queue.",
                    cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public Task OnRollbackAsync(CancellationToken cancellationToken = default)
    {
        _buffer.Drain();
        return Task.CompletedTask;
    }
}
