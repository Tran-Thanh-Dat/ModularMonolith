using BuildingBlocks.Application.Caching;
using BuildingBlocks.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuildingBlocks.Infrastructure.Tests.Caching;

public sealed class NoCacheServiceTests
{
    [Fact]
    public async Task GetOrSetAsync_AlwaysCallsFactory()
    {
        var cache = new NoCacheService();
        var callCount = 0;

        async Task<int> Factory(CancellationToken _) =>
            await Task.FromResult(++callCount);

        var first = await cache.GetOrSetAsync("key", Factory);
        var second = await cache.GetOrSetAsync("key", Factory);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
    }
}

public sealed class MemoryCacheServiceTests
{
    [Fact]
    public async Task RemoveByPrefix_RemovesOnlyMatchingKeys()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registry = new CacheKeyRegistry();
        var options = Options.Create(new CacheOptions
        {
            KeyPrefix = "test",
            DefaultExpirationMinutes = 30
        });

        var cache = new MemoryCacheService(
            memoryCache,
            registry,
            options,
            NullLogger<MemoryCacheService>.Instance);

        await cache.SetAsync("v1:categories:list:aaa", "list-a");
        await cache.SetAsync("v1:categories:list:bbb", "list-b");
        await cache.SetAsync("v1:categories:detail:111", "detail");

        await cache.RemoveByPrefixAsync(CacheKeys.CategoryListPrefix);

        Assert.False(await cache.ExistsAsync("v1:categories:list:aaa"));
        Assert.False(await cache.ExistsAsync("v1:categories:list:bbb"));
        Assert.True(await cache.ExistsAsync("v1:categories:detail:111"));
    }

    [Fact]
    public async Task SetAsync_SkipsSensitiveKeyNames()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registry = new CacheKeyRegistry();
        var options = Options.Create(new CacheOptions
        {
            KeyPrefix = "test",
            DefaultExpirationMinutes = 30,
            EnableLogging = false
        });

        var cache = new MemoryCacheService(
            memoryCache,
            registry,
            options,
            NullLogger<MemoryCacheService>.Instance);

        await cache.SetAsync("user:password:hash", "secret-value");
        await cache.SetAsync("user:profile:1", "safe-value");

        Assert.False(await cache.ExistsAsync("user:password:hash"));
        Assert.True(await cache.ExistsAsync("user:profile:1"));
    }
}

public sealed class CacheOperationBufferTests
{
    [Fact]
    public void Clear_RemovesQueuedOperations()
    {
        var buffer = new CacheOperationBuffer();

        buffer.EnqueueSet("key", "value");
        buffer.EnqueueRemove("other-key");
        buffer.Clear();

        Assert.Empty(buffer.TakeAll());
    }

    [Fact]
    public async Task PostCommitHook_FlushesSetAndRemoveOperations()
    {
        var buffer = new CacheOperationBuffer();
        var cache = new RecordingCacheService();
        var hook = new CacheInvalidationPostCommitHook(
            buffer,
            cache,
            NullLogger<CacheInvalidationPostCommitHook>.Instance);

        buffer.EnqueueSet("permissions", new[] { "Users.View" }, TimeSpan.FromMinutes(10));
        buffer.EnqueueRemove("stale-key");

        await hook.OnCommittedAsync();

        Assert.Single(cache.SetOperations);
        Assert.Equal("permissions", cache.SetOperations[0].Key);
        Assert.Equal(new[] { "Users.View" }, cache.SetOperations[0].Value);
        Assert.Equal(TimeSpan.FromMinutes(10), cache.SetOperations[0].Expiration);
        Assert.Equal(["stale-key"], cache.RemovedKeys);
        Assert.Empty(buffer.TakeAll());
    }

    [Fact]
    public async Task PostCommitHook_RollbackClearsQueuedOperations()
    {
        var buffer = new CacheOperationBuffer();
        var cache = new RecordingCacheService();
        var hook = new CacheInvalidationPostCommitHook(
            buffer,
            cache,
            NullLogger<CacheInvalidationPostCommitHook>.Instance);

        buffer.EnqueueSet("permissions", new[] { "Users.View" });
        await hook.OnRollbackAsync();
        await hook.OnCommittedAsync();

        Assert.Empty(cache.SetOperations);
        Assert.Empty(cache.RemovedKeys);
    }
}

public sealed class CacheKeysTests
{
    [Fact]
    public void HashQueryParameters_IsStableForSameInput()
    {
        var first = CacheKeys.HashQueryParameters("abc", true, 1, 20);
        var second = CacheKeys.HashQueryParameters("abc", true, 1, 20);

        Assert.Equal(first, second);
    }

    [Fact]
    public void HashQueryParameters_ChangesWhenInputChanges()
    {
        var first = CacheKeys.HashQueryParameters("abc", true, 1, 20);
        var second = CacheKeys.HashQueryParameters("abc", false, 1, 20);

        Assert.NotEqual(first, second);
    }
}

internal sealed class RecordingCacheService : ICacheService
{
    public List<(string Key, object? Value, TimeSpan? Expiration)> SetOperations { get; } = [];

    public List<string> RemovedKeys { get; } = [];

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult<T?>(default);

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        SetOperations.Add((key, value, expiration));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        RemovedKeys.Add(key);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) =>
        factory(cancellationToken);

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
