using BuildingBlocks.Application.Caching;

namespace BuildingBlocks.Infrastructure.Caching;

internal sealed class CacheOperationBuffer : ICacheInvalidationBuffer
{
    private readonly List<CacheOperationEntry> _entries = [];

    public void EnqueueSet<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key) || value is null)
        {
            return;
        }

        _entries.Add(new CacheOperationEntry
        {
            OperationType = CacheOperationType.Set,
            Key = key,
            Value = value,
            ValueType = typeof(T),
            Expiration = expiration
        });
    }

    public void EnqueueRemove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _entries.Add(new CacheOperationEntry
        {
            OperationType = CacheOperationType.Remove,
            Key = key
        });
    }

    public void EnqueueRemoveByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            return;
        }

        _entries.Add(new CacheOperationEntry
        {
            OperationType = CacheOperationType.RemoveByPrefix,
            Prefix = prefix
        });
    }

    public void Clear() => _entries.Clear();

    public IReadOnlyCollection<CacheOperationEntry> TakeAll()
    {
        if (_entries.Count == 0)
        {
            return Array.Empty<CacheOperationEntry>();
        }

        var snapshot = _entries.ToArray();
        _entries.Clear();
        return snapshot;
    }
}
