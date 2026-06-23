using BuildingBlocks.Application.Caching;

namespace BuildingBlocks.Testing.Fakes;

public sealed class RecordingCacheService : ICacheService
{
    private readonly Dictionary<string, object?> _store = new(StringComparer.Ordinal);

    public List<(string Key, object? Value, TimeSpan? Expiration)> SetOperations { get; } = [];

    public List<string> RemovedKeys { get; } = [];

    public List<string> RemovedPrefixes { get; } = [];

    public int GetCallCount { get; set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        return Task.FromResult(_store.TryGetValue(key, out var value) ? (T?)value : default);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        SetOperations.Add((key, value, expiration));
        _store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemovedKeys.Add(key);
        _store.Remove(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        RemovedPrefixes.Add(prefix);
        var keysToRemove = _store.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList();
        foreach (var key in keysToRemove)
        {
            _store.Remove(key);
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        GetCallCount++;
        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, expiration, cancellationToken);
        return value;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_store.ContainsKey(key));
}
