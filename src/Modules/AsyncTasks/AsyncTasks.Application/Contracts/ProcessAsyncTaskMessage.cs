namespace AsyncTasks.Application.Contracts;

public sealed record ProcessAsyncTaskMessage
{
    public Guid MessageId { get; init; }

    public Guid CorrelationId { get; init; }

    public Guid AsyncTaskId { get; init; }

    public string TaskNo { get; init; } = default!;

    public string TaskType { get; init; } = default!;

    public string? Payload { get; init; }

    public Guid? RequestedByUserId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
