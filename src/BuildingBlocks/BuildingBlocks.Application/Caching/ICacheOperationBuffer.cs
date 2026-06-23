namespace BuildingBlocks.Application.Caching;

public enum CacheOperationType
{
    Set,
    Remove,
    RemoveByPrefix
}

public sealed class CacheOperationEntry
{
    public CacheOperationType OperationType { get; init; }

    public string? Key { get; init; }

    public string? Prefix { get; init; }

    public object? Value { get; init; }

    public Type? ValueType { get; init; }

    public TimeSpan? Expiration { get; init; }
}

public interface ICacheOperationBuffer
{
    void EnqueueSet<T>(string key, T value, TimeSpan? expiration = null);

    void EnqueueRemove(string key);

    void EnqueueRemoveByPrefix(string prefix);

    void Clear();

    IReadOnlyCollection<CacheOperationEntry> TakeAll();
}

/// <summary>
/// Backward-compatible alias for invalidation-only callers.
/// </summary>
public interface ICacheInvalidationBuffer : ICacheOperationBuffer;
