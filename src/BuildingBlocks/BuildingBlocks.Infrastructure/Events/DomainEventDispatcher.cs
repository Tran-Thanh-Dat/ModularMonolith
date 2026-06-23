using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Domain.Events;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Events;

public sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Domain event dispatched. EventId: {EventId}, EventType: {EventType}, OccurredAt: {OccurredAt}",
                domainEvent.EventId,
                domainEvent.GetType().Name,
                domainEvent.OccurredAt);
        }

        return Task.CompletedTask;
    }
}
