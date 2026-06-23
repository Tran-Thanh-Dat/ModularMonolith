namespace BuildingBlocks.Infrastructure.Outbox;

public abstract class OutboxMessage
{
    public Guid Id { get; protected set; }

    public string Type { get; protected set; } = default!;

    public string Content { get; protected set; } = default!;

    public DateTimeOffset OccurredOn { get; protected set; }

    public DateTimeOffset? ProcessedOn { get; protected set; }

    public string? Error { get; protected set; }

    public int RetryCount { get; protected set; }

    protected OutboxMessage()
    {
    }

    protected OutboxMessage(Guid id, string type, string content, DateTimeOffset occurredOn)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredOn = occurredOn;
    }

    public void MarkAsProcessed(DateTimeOffset processedOn)
    {
        ProcessedOn = processedOn;
        Error = null;
    }

    public void MarkAsFailed(string error, DateTimeOffset failedAt)
    {
        RetryCount++;
        Error = error;
        ProcessedOn = failedAt;
    }
}
