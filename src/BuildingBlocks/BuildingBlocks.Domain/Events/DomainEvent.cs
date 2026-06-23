namespace BuildingBlocks.Domain.Events;

public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTimeOffset.UtcNow;
    }

    protected DomainEvent(Guid eventId, DateTimeOffset occurredAt)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
    }

    public Guid EventId { get; init; }

    public DateTimeOffset OccurredAt { get; init; }
}
