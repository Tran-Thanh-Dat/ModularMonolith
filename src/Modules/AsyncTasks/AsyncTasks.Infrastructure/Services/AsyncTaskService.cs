using System.Text.Json;
using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Constants;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Application.Dtos;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Entities;
using AsyncTasks.Domain.Errors;
using AsyncTasks.Infrastructure.Options;
using AsyncTasks.Infrastructure.Persistence;
using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsyncTasks.Infrastructure.Services;

public sealed class AsyncTaskService : IAsyncTaskService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly AsyncTasksUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly IAsyncTaskPublishBuffer _publishBuffer;
    private readonly MessageQueueOptions _queueOptions;
    private readonly ILogger<AsyncTaskService> _logger;

    public AsyncTaskService(
        AsyncTasksUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        IAsyncTaskPublishBuffer publishBuffer,
        IOptions<MessageQueueOptions> queueOptions,
        ILogger<AsyncTaskService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _publishBuffer = publishBuffer;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    public Task<AsyncTaskDto> SubmitEmailDemoAsync(
        string? emailTo,
        string? subject,
        string? body,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        SubmitInternalAsync(
            AsyncTaskTypes.EmailDemo,
            JsonSerializer.Serialize(new { emailTo, subject, body }, JsonOptions),
            tenantId,
            organizationId,
            cancellationToken);

    public Task<AsyncTaskDto> SubmitFileProcessingDemoAsync(
        Guid? fileId,
        string? fileName,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        SubmitInternalAsync(
            AsyncTaskTypes.FileProcessingDemo,
            JsonSerializer.Serialize(new { fileId, fileName }, JsonOptions),
            tenantId,
            organizationId,
            cancellationToken);

    public Task<AsyncTaskDto> SubmitFailDemoAsync(
        string? failReason,
        bool shouldAlwaysFail,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        SubmitInternalAsync(
            AsyncTaskTypes.FailDemo,
            JsonSerializer.Serialize(new { failReason, shouldAlwaysFail }, JsonOptions),
            tenantId,
            organizationId,
            cancellationToken);

    public Task<AsyncTaskDto> SubmitLongRunningDemoAsync(
        int durationSeconds,
        int steps,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default) =>
        SubmitInternalAsync(
            AsyncTaskTypes.LongRunningDemo,
            JsonSerializer.Serialize(new { durationSeconds, steps }, JsonOptions),
            tenantId,
            organizationId,
            cancellationToken);

    public async Task<AsyncTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await FindTaskReadOnlyAsync(id, cancellationToken);
        return task is null ? null : MapToDto(task);
    }

    public async Task<PagedResult<AsyncTaskDto>> GetPagedAsync(
        AsyncTaskListFilter filter,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest
        {
            PageIndex = filter.PageIndex,
            PageSize = filter.PageSize,
            Keyword = filter.Keyword
        };

        var query = _unitOfWork.Repository<AsyncTask, Guid>()
            .QueryReadOnly()
            .Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(filter.TaskNo))
        {
            query = query.Where(t => t.TaskNo == filter.TaskNo.Trim());
        }

        if (!string.IsNullOrWhiteSpace(filter.Type))
        {
            query = query.Where(t => t.Type == filter.Type.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(t => t.Status == filter.Status.Trim().ToUpperInvariant());
        }

        if (filter.RequestedByUserId.HasValue)
        {
            query = query.Where(t => t.RequestedByUserId == filter.RequestedByUserId);
        }

        if (filter.TenantId.HasValue)
        {
            query = query.Where(t => t.TenantId == filter.TenantId);
        }

        if (filter.OrganizationId.HasValue)
        {
            query = query.Where(t => t.OrganizationId == filter.OrganizationId);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.CreatedTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Keyword))
        {
            var pattern = $"%{filter.Keyword.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.TaskNo, pattern) ||
                EF.Functions.ILike(t.Type, pattern) ||
                EF.Functions.ILike(t.Status, pattern));
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AsyncTaskDto
            {
                Id = t.Id,
                TaskNo = t.TaskNo,
                Type = t.Type,
                Status = t.Status,
                Payload = t.Payload,
                Result = t.Result,
                RetryCount = t.RetryCount,
                MaxRetryCount = t.MaxRetryCount,
                ProgressPercent = t.ProgressPercent,
                CorrelationId = t.CorrelationId,
                QueueName = t.QueueName,
                ConsumerName = t.ConsumerName,
                LastErrorCode = t.LastErrorCode,
                LastErrorMessage = t.LastErrorMessage,
                StartedAt = t.StartedAt,
                CompletedAt = t.CompletedAt,
                FailedAt = t.FailedAt,
                CancelledAt = t.CancelledAt,
                TenantId = t.TenantId,
                OrganizationId = t.OrganizationId,
                RequestedByUserId = t.RequestedByUserId,
                CreatedAt = t.CreatedAt
            })
            .ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        task.MarkCancelled(_dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Cancelled,
            $"Async task '{task.TaskNo}' was cancelled.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);
    }

    public async Task<AsyncTaskDto> RetryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureQueueEnabled();

        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        var newMessageId = Guid.NewGuid();
        task.PrepareRetry(newMessageId, _dateTimeProvider.UtcNow, _currentUserService.UserId);

        var message = BuildMessage(task);
        _publishBuffer.Enqueue(message);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Retried,
            $"Async task '{task.TaskNo}' was queued for retry.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);

        return MapToDto(task);
    }

    public async Task MarkQueuedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        if (task.Status == AsyncTaskStatuses.Queued)
        {
            return;
        }

        task.MarkQueued(_dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Queued,
            $"Async task '{task.TaskNo}' was queued.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);
    }

    public async Task MarkProcessingAsync(Guid id, string consumerName, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        if (task.Status is AsyncTaskStatuses.Completed or AsyncTaskStatuses.Cancelled or AsyncTaskStatuses.Failed)
        {
            return;
        }

        task.MarkProcessing(consumerName, _dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Started,
            $"Async task '{task.TaskNo}' started processing.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);
    }

    public async Task MarkCompletedAsync(Guid id, string? result, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        if (task.Status == AsyncTaskStatuses.Completed)
        {
            return;
        }

        task.MarkCompleted(result, _dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Completed,
            $"Async task '{task.TaskNo}' completed.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, string errorCode, string errorMessage, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        if (task.Status == AsyncTaskStatuses.Failed)
        {
            return;
        }

        task.MarkFailed(errorCode, errorMessage, _dateTimeProvider.UtcNow, _currentUserService.UserId);

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Failed,
            $"Async task '{task.TaskNo}' failed.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);
    }

    public async Task UpdateProgressAsync(Guid id, int progressPercent, CancellationToken cancellationToken = default)
    {
        var task = await GetTaskForUpdateAsync(id, cancellationToken);
        task.UpdateProgress(progressPercent, _dateTimeProvider.UtcNow, _currentUserService.UserId);
    }

    private async Task<AsyncTaskDto> SubmitInternalAsync(
        string taskType,
        string payload,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken)
    {
        EnsureQueueEnabled();

        var now = _dateTimeProvider.UtcNow;
        var messageId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        var taskNo = GenerateTaskNo(now);

        var task = AsyncTask.CreatePending(
            taskNo,
            taskType,
            payload,
            _queueOptions.Retry.RetryCount,
            messageId,
            correlationId,
            AsyncTasksConstants.ProcessQueueName,
            tenantId,
            organizationId,
            _currentUserService.UserId,
            now,
            _currentUserService.UserId);

        await _unitOfWork.Repository<AsyncTask, Guid>().AddAsync(task, cancellationToken);
        _publishBuffer.Enqueue(BuildMessage(task));

        await _activityLogService.EnqueuePostCommitAsync(
            AsyncTaskActivityTypes.Submitted,
            $"Async task '{task.TaskNo}' submitted.",
            AuditLogConstants.Modules.AsyncTasks,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Submitted async task {TaskId} {TaskNo} type {TaskType}", task.Id, task.TaskNo, task.Type);
        return MapToDto(task);
    }

    private void EnsureQueueEnabled()
    {
        if (!_queueOptions.Enabled)
        {
            throw new BadRequestException(
                AsyncTaskErrors.MessageQueueDisabled,
                "Message queue is disabled. Enable MessageQueue:Enabled to submit async tasks.");
        }
    }

    private async Task<AsyncTask?> FindTaskReadOnlyAsync(Guid id, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<AsyncTask, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

    private async Task<AsyncTask> GetTaskForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var task = await _unitOfWork.Repository<AsyncTask, Guid>()
            .Query()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (task is null)
        {
            throw new NotFoundException(AsyncTaskErrors.NotFound, $"Async task '{id}' was not found.");
        }

        return task;
    }

    private static ProcessAsyncTaskMessage BuildMessage(AsyncTask task) =>
        new()
        {
            MessageId = task.MessageId ?? Guid.NewGuid(),
            CorrelationId = task.CorrelationId ?? Guid.NewGuid(),
            AsyncTaskId = task.Id,
            TaskNo = task.TaskNo,
            TaskType = task.Type,
            Payload = task.Payload,
            RequestedByUserId = task.RequestedByUserId,
            TenantId = task.TenantId,
            OrganizationId = task.OrganizationId,
            CreatedAt = task.CreatedAt
        };

    private static string GenerateTaskNo(DateTimeOffset now) =>
        $"AT-{now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private static AsyncTaskDto MapToDto(AsyncTask task) =>
        new()
        {
            Id = task.Id,
            TaskNo = task.TaskNo,
            Type = task.Type,
            Status = task.Status,
            Payload = task.Payload,
            Result = task.Result,
            RetryCount = task.RetryCount,
            MaxRetryCount = task.MaxRetryCount,
            ProgressPercent = task.ProgressPercent,
            CorrelationId = task.CorrelationId,
            QueueName = task.QueueName,
            ConsumerName = task.ConsumerName,
            LastErrorCode = task.LastErrorCode,
            LastErrorMessage = task.LastErrorMessage,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            FailedAt = task.FailedAt,
            CancelledAt = task.CancelledAt,
            TenantId = task.TenantId,
            OrganizationId = task.OrganizationId,
            RequestedByUserId = task.RequestedByUserId,
            CreatedAt = task.CreatedAt
        };
}
