using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Caching;

internal sealed class MemoryCacheService : CacheServiceBase, ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ICacheKeyRegistry _keyRegistry;

    public MemoryCacheService(
        IMemoryCache memoryCache,
        ICacheKeyRegistry keyRegistry,
        IOptions<CacheOptions> options,
        ILogger<MemoryCacheService> logger)
        : base(options, logger)
    {
        _memoryCache = memoryCache;
        _keyRegistry = keyRegistry;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            if (_memoryCache.TryGetValue(storageKey, out var cached) && cached is T typed)
            {
                return Task.FromResult<T?>(typed);
            }

            return Task.FromResult<T?>(default);
        }
        catch (Exception ex)
        {
            LogCacheFailure("get", key, ex);
            return Task.FromResult<T?>(default);
        }
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        if (!CacheSensitiveDataGuard.CanCache<T>(Logger, key, Options.EnableLogging))
        {
            return Task.CompletedTask;
        }

        try
        {
            var storageKey = BuildStorageKey(key);
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResolveExpiration(expiration)
            };

            options.RegisterPostEvictionCallback((evictedKey, _, _, _) =>
            {
                if (evictedKey is string trackedKey)
                {
                    _keyRegistry.Untrack(trackedKey);
                }
            });

            _memoryCache.Set(storageKey, value, options);
            _keyRegistry.Track(storageKey);

            if (Options.EnableLogging)
            {
                Logger.LogDebug("Memory cache set for key {CacheKey}", key);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("set", key, ex);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            _memoryCache.Remove(storageKey);
            _keyRegistry.Untrack(storageKey);

            if (Options.EnableLogging)
            {
                Logger.LogDebug("Memory cache removed key {CacheKey}", key);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("remove", key, ex);
        }

        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var storagePrefix = BuildStorageKey(prefix);
            var keys = _keyRegistry.GetKeysByPrefix(storagePrefix);

            foreach (var storageKey in keys)
            {
                _memoryCache.Remove(storageKey);
            }

            _keyRegistry.RemoveByPrefix(storagePrefix);

            if (Options.EnableLogging)
            {
                Logger.LogDebug(
                    "Memory cache removed {Count} keys with prefix {CachePrefix}",
                    keys.Count,
                    prefix);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("remove-by-prefix", prefix, ex);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            return Task.FromResult(_memoryCache.TryGetValue(storageKey, out _));
        }
        catch (Exception ex)
        {
            LogCacheFailure("exists", key, ex);
            return Task.FromResult(false);
        }
    }

    public Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        GetOrSetCoreAsync(this, key, factory, expiration, cancellationToken);
}
