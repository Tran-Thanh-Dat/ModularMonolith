namespace AsyncTasks.Application.Dtos;

public sealed class AsyncTaskDto
{
    public Guid Id { get; init; }

    public string TaskNo { get; init; } = default!;

    public string Type { get; init; } = default!;

    public string Status { get; init; } = default!;

    public string? Payload { get; init; }

    public string? Result { get; init; }

    public int RetryCount { get; init; }

    public int MaxRetryCount { get; init; }

    public int ProgressPercent { get; init; }

    public Guid? CorrelationId { get; init; }

    public string? QueueName { get; init; }

    public string? ConsumerName { get; init; }

    public string? LastErrorCode { get; init; }

    public string? LastErrorMessage { get; init; }

    public DateTimeOffset? StartedAt { get; init; }

    public DateTimeOffset? CompletedAt { get; init; }

    public DateTimeOffset? FailedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? RequestedByUserId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
