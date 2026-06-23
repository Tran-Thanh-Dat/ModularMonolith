using BuildingBlocks.Application.Caching;

namespace BuildingBlocks.Testing.Fakes;

public sealed class RecordingCacheInvalidationBuffer : ICacheInvalidationBuffer
{
    private readonly List<CacheOperationEntry> _pending = [];

    public List<string> RemovedKeys { get; } = [];

    public List<string> RemovedPrefixes { get; } = [];

    public List<(string Key, object? Value, TimeSpan? Expiration)> SetOperations { get; } = [];

    public void EnqueueRemove(string key)
    {
        RemovedKeys.Add(key);
        _pending.Add(new CacheOperationEntry { OperationType = CacheOperationType.Remove, Key = key });
    }

    public void EnqueueRemoveByPrefix(string prefix)
    {
        RemovedPrefixes.Add(prefix);
        _pending.Add(new CacheOperationEntry { OperationType = CacheOperationType.RemoveByPrefix, Prefix = prefix });
    }

    public void EnqueueSet<T>(string key, T value, TimeSpan? expiration = null)
    {
        SetOperations.Add((key, value, expiration));
        _pending.Add(new CacheOperationEntry
        {
            OperationType = CacheOperationType.Set,
            Key = key,
            Value = value,
            ValueType = typeof(T),
            Expiration = expiration
        });
    }

    public void Clear() => _pending.Clear();

    public IReadOnlyCollection<CacheOperationEntry> TakeAll()
    {
        var snapshot = _pending.ToArray();
        _pending.Clear();
        return snapshot;
    }
}
