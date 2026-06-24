using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Errors;

namespace AsyncTasks.Domain.Entities;

public sealed class AsyncTask : SoftDeletableEntity
{
    private AsyncTask()
    {
    }

    private AsyncTask(
        Guid id,
        string taskNo,
        string type,
        string status,
        string? payload,
        int maxRetryCount,
        Guid messageId,
        Guid correlationId,
        string queueName,
        Guid? tenantId,
        Guid? organizationId,
        Guid? requestedByUserId,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        TaskNo = taskNo;
        Type = type;
        Status = status;
        Payload = payload;
        MaxRetryCount = maxRetryCount;
        RetryCount = 0;
        ProgressPercent = 0;
        MessageId = messageId;
        CorrelationId = correlationId;
        QueueName = queueName;
        TenantId = tenantId;
        OrganizationId = organizationId;
        RequestedByUserId = requestedByUserId;
        SetCreated(createdBy, createdAt);
    }

    public string TaskNo { get; private set; } = default!;

    public string Type { get; private set; } = default!;

    public string Status { get; private set; } = default!;

    public string? Payload { get; private set; }

    public string? Result { get; private set; }

    public int RetryCount { get; private set; }

    public int MaxRetryCount { get; private set; }

    public int ProgressPercent { get; private set; }

    public Guid? MessageId { get; private set; }

    public Guid? CorrelationId { get; private set; }

    public string? QueueName { get; private set; }

    public string? ConsumerName { get; private set; }

    public string? LastErrorCode { get; private set; }

    public string? LastErrorMessage { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset? FailedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    public Guid? TenantId { get; private set; }

    public Guid? OrganizationId { get; private set; }

    public Guid? RequestedByUserId { get; private set; }

    public static AsyncTask CreatePending(
        string taskNo,
        string type,
        string? payload,
        int maxRetryCount,
        Guid messageId,
        Guid correlationId,
        string queueName,
        Guid? tenantId,
        Guid? organizationId,
        Guid? requestedByUserId,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        ValidateType(type);
        ValidatePayload(payload);

        if (string.IsNullOrWhiteSpace(taskNo))
        {
            throw new DomainException(AsyncTaskErrors.TaskNoAlreadyExists, "Task number is required.");
        }

        return new AsyncTask(
            Guid.NewGuid(),
            taskNo.Trim(),
            type.Trim().ToUpperInvariant(),
            AsyncTaskStatuses.Pending,
            payload,
            maxRetryCount,
            messageId,
            correlationId,
            queueName,
            tenantId,
            organizationId,
            requestedByUserId,
            createdAt,
            createdBy);
    }

    public void MarkQueued(DateTimeOffset at, Guid? updatedBy)
    {
        EnsureNotTerminal();
        Status = AsyncTaskStatuses.Queued;
        SetUpdated(updatedBy, at);
    }

    public void MarkProcessing(string consumerName, DateTimeOffset at, Guid? updatedBy)
    {
        if (Status is AsyncTaskStatuses.Completed or AsyncTaskStatuses.Cancelled)
        {
            return;
        }

        Status = AsyncTaskStatuses.Processing;
        ConsumerName = consumerName;
        StartedAt ??= at;
        SetUpdated(updatedBy, at);
    }

    public void MarkCompleted(string? result, DateTimeOffset at, Guid? updatedBy)
    {
        Status = AsyncTaskStatuses.Completed;
        Result = result;
        ProgressPercent = 100;
        CompletedAt = at;
        LastErrorCode = null;
        LastErrorMessage = null;
        SetUpdated(updatedBy, at);
    }

    public void MarkFailed(string errorCode, string errorMessage, DateTimeOffset at, Guid? updatedBy)
    {
        Status = AsyncTaskStatuses.Failed;
        LastErrorCode = errorCode;
        LastErrorMessage = Truncate(errorMessage, AsyncTasksConstants.MaxErrorMessageLength);
        FailedAt = at;
        SetUpdated(updatedBy, at);
    }

    public void MarkCancelled(DateTimeOffset at, Guid? updatedBy)
    {
        if (!AsyncTaskStatuses.Cancellable.Contains(Status))
        {
            throw new DomainException(AsyncTaskErrors.CannotCancel, "Task cannot be cancelled in current status.");
        }

        Status = AsyncTaskStatuses.Cancelled;
        CancelledAt = at;
        SetUpdated(updatedBy, at);
    }

    public void PrepareRetry(Guid newMessageId, DateTimeOffset at, Guid? updatedBy)
    {
        if (Status != AsyncTaskStatuses.Failed)
        {
            throw new DomainException(AsyncTaskErrors.CannotRetry, "Only failed tasks can be retried.");
        }

        if (RetryCount >= MaxRetryCount)
        {
            throw new DomainException(AsyncTaskErrors.MaxRetryExceeded, "Maximum retry count exceeded.");
        }

        RetryCount++;
        Status = AsyncTaskStatuses.Pending;
        MessageId = newMessageId;
        LastErrorCode = null;
        LastErrorMessage = null;
        FailedAt = null;
        CompletedAt = null;
        StartedAt = null;
        ProgressPercent = 0;
        SetUpdated(updatedBy, at);
    }

    public void UpdateProgress(int progressPercent, DateTimeOffset at, Guid? updatedBy)
    {
        if (progressPercent is < 0 or > 100)
        {
            throw new DomainException(AsyncTaskErrors.InvalidProgress, "Progress must be between 0 and 100.");
        }

        ProgressPercent = progressPercent;
        SetUpdated(updatedBy, at);
    }

    public void IncrementRetryCount(DateTimeOffset at, Guid? updatedBy)
    {
        RetryCount++;
        SetUpdated(updatedBy, at);
    }

    private void EnsureNotTerminal()
    {
        if (AsyncTaskStatuses.Terminal.Contains(Status))
        {
            throw new DomainException(AsyncTaskErrors.InvalidStatus, "Task is in terminal status.");
        }
    }

    private static void ValidateType(string type)
    {
        if (string.IsNullOrWhiteSpace(type) || !AsyncTaskTypes.All.Contains(type.Trim()))
        {
            throw new DomainException(AsyncTaskErrors.InvalidType, "Invalid async task type.");
        }
    }

    private static void ValidatePayload(string? payload)
    {
        if (payload is not null && payload.Length > AsyncTasksConstants.MaxPayloadLength)
        {
            throw new DomainException(AsyncTaskErrors.PayloadTooLarge, "Payload is too large.");
        }
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
