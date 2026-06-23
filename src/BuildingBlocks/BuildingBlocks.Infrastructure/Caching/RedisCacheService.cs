using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BuildingBlocks.Infrastructure.Caching;

internal sealed class RedisCacheService : CacheServiceBase, ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer? _connectionMultiplexer;
    private readonly ICacheKeyRegistry _keyRegistry;

    public RedisCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer? connectionMultiplexer,
        ICacheKeyRegistry keyRegistry,
        IOptions<CacheOptions> options,
        ILogger<RedisCacheService> logger)
        : base(options, logger)
    {
        _distributedCache = distributedCache;
        _connectionMultiplexer = connectionMultiplexer;
        _keyRegistry = keyRegistry;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            var bytes = await _distributedCache.GetAsync(storageKey, cancellationToken);
            if (bytes is null || bytes.Length == 0)
            {
                return default;
            }

            return CacheJsonSerializer.Deserialize<T>(bytes);
        }
        catch (Exception ex)
        {
            LogSerializationFailure("get", key, ex);
            return default;
        }
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            return;
        }

        if (!CacheSensitiveDataGuard.CanCache<T>(Logger, key, Options.EnableLogging))
        {
            return;
        }

        try
        {
            var storageKey = BuildStorageKey(key);
            var bytes = CacheJsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = ResolveExpiration(expiration)
            };

            await _distributedCache.SetAsync(storageKey, bytes, options, cancellationToken);
            _keyRegistry.Track(storageKey);

            if (Options.EnableLogging)
            {
                Logger.LogDebug("Redis cache set for key {CacheKey}", key);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("set", key, ex);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            await _distributedCache.RemoveAsync(storageKey, cancellationToken);
            _keyRegistry.Untrack(storageKey);

            if (Options.EnableLogging)
            {
                Logger.LogDebug("Redis cache removed key {CacheKey}", key);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("remove", key, ex);
        }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        try
        {
            var storagePrefix = BuildStorageKey(prefix);

            if (_connectionMultiplexer is not null)
            {
                await RemoveByPrefixUsingScanAsync(storagePrefix, cancellationToken);
            }
            else
            {
                await RemoveByPrefixUsingRegistryAsync(storagePrefix, cancellationToken);
            }

            _keyRegistry.RemoveByPrefix(storagePrefix);

            if (Options.EnableLogging)
            {
                Logger.LogDebug("Redis cache removed keys with prefix {CachePrefix}", prefix);
            }
        }
        catch (Exception ex)
        {
            LogCacheFailure("remove-by-prefix", prefix, ex);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var storageKey = BuildStorageKey(key);
            var bytes = await _distributedCache.GetAsync(storageKey, cancellationToken);
            return bytes is not null && bytes.Length > 0;
        }
        catch (Exception ex)
        {
            LogCacheFailure("exists", key, ex);
            return false;
        }
    }

    public Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        GetOrSetCoreAsync(this, key, factory, expiration, cancellationToken);

    private async Task RemoveByPrefixUsingScanAsync(string storagePrefix, CancellationToken cancellationToken)
    {
        var endpoints = _connectionMultiplexer!.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _connectionMultiplexer.GetServer(endpoint);
            if (!server.IsConnected || server.IsReplica)
            {
                continue;
            }

            await foreach (var key in server.KeysAsync(pattern: $"{storagePrefix}*"))
            {
                cancellationToken.ThrowIfCancellationRequested();
                await _distributedCache.RemoveAsync(key.ToString(), cancellationToken);
            }
        }
    }

    private async Task RemoveByPrefixUsingRegistryAsync(string storagePrefix, CancellationToken cancellationToken)
    {
        var keys = _keyRegistry.GetKeysByPrefix(storagePrefix);
        foreach (var storageKey in keys)
        {
            await _distributedCache.RemoveAsync(storageKey, cancellationToken);
        }
    }
}
