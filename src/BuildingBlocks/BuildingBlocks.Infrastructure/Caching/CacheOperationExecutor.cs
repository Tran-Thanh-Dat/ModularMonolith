using BuildingBlocks.Application.Caching;

namespace BuildingBlocks.Infrastructure.Caching;

internal static class CacheOperationExecutor
{
    public static async Task ExecuteAsync(
        ICacheService cacheService,
        CacheOperationEntry entry,
        CancellationToken cancellationToken)
    {
        switch (entry.OperationType)
        {
            case CacheOperationType.Set:
                await ExecuteSetAsync(cacheService, entry, cancellationToken);
                break;

            case CacheOperationType.Remove:
                await cacheService.RemoveAsync(entry.Key!, cancellationToken);
                break;

            case CacheOperationType.RemoveByPrefix:
                await cacheService.RemoveByPrefixAsync(entry.Prefix!, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported cache operation type '{entry.OperationType}'.");
        }
    }

    private static Task ExecuteSetAsync(
        ICacheService cacheService,
        CacheOperationEntry entry,
        CancellationToken cancellationToken)
    {
        if (entry.Value is null || entry.ValueType is null || entry.Key is null)
        {
            return Task.CompletedTask;
        }

        var setMethod = typeof(ICacheService)
            .GetMethod(nameof(ICacheService.SetAsync))
            ?.MakeGenericMethod(entry.ValueType);

        if (setMethod is null)
        {
            throw new InvalidOperationException("Unable to resolve cache set operation.");
        }

        return (Task)setMethod.Invoke(
            cacheService,
            [entry.Key, entry.Value, entry.Expiration, cancellationToken])!;
    }
}
