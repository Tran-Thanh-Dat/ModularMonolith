namespace BuildingBlocks.Infrastructure.Caching;

internal interface ICacheKeyRegistry
{
    void Track(string storageKey);

    void Untrack(string storageKey);

    IReadOnlyCollection<string> GetKeysByPrefix(string prefix);

    void RemoveByPrefix(string prefix);
}

internal sealed class CacheKeyRegistry : ICacheKeyRegistry
{
    private readonly HashSet<string> _keys = new(StringComparer.Ordinal);
    private readonly object _sync = new();

    public void Track(string storageKey)
    {
        lock (_sync)
        {
            _keys.Add(storageKey);
        }
    }

    public void Untrack(string storageKey)
    {
        lock (_sync)
        {
            _keys.Remove(storageKey);
        }
    }

    public IReadOnlyCollection<string> GetKeysByPrefix(string prefix)
    {
        lock (_sync)
        {
            return _keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray();
        }
    }

    public void RemoveByPrefix(string prefix)
    {
        lock (_sync)
        {
            var toRemove = _keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToArray();

            foreach (var key in toRemove)
            {
                _keys.Remove(key);
            }
        }
    }
}
