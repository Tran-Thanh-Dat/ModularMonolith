using System.Text.Json;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Caching;

internal static class CacheJsonSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static byte[] Serialize<T>(T value) =>
        JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

    public static T? Deserialize<T>(byte[] bytes) =>
        JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
}

internal static class CacheSensitiveDataGuard
{
    private static readonly HashSet<string> BlockedPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "RefreshToken",
        "AccessToken",
        "Token",
        "Secret",
        "ApiKey"
    };

    public static bool CanCache<T>(ILogger logger, string key, bool enableLogging)
    {
        var typeName = typeof(T).Name;
        if (ContainsSensitiveName(typeName) || ContainsSensitiveName(key))
        {
            if (enableLogging)
            {
                logger.LogWarning(
                    "Skipping cache write for key {CacheKey} because the payload may contain sensitive data.",
                    key);
            }

            return false;
        }

        return true;
    }

    private static bool ContainsSensitiveName(string value)
    {
        foreach (var blocked in BlockedPropertyNames)
        {
            if (value.Contains(blocked, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

internal abstract class CacheServiceBase
{
    protected CacheServiceBase(
        IOptions<CacheOptions> options,
        ILogger logger)
    {
        Options = options.Value;
        Logger = logger;
    }

    protected CacheOptions Options { get; }

    protected ILogger Logger { get; }

    protected TimeSpan ResolveExpiration(TimeSpan? expiration) =>
        expiration ?? TimeSpan.FromMinutes(Options.DefaultExpirationMinutes);

    protected string BuildStorageKey(string key) =>
        CacheKeys.ApplyInstanceName(
            CacheKeys.ApplyConfiguredPrefix(key, Options.KeyPrefix),
            Options.Redis.InstanceName);

    protected void LogCacheFailure(string operation, string key, Exception exception)
    {
        Logger.LogWarning(
            exception,
            "Cache {Operation} failed for key {CacheKey}. ErrorCode={ErrorCode}",
            operation,
            key,
            CacheErrors.ConnectionFailed);
    }

    protected void LogSerializationFailure(string operation, string key, Exception exception)
    {
        Logger.LogWarning(
            exception,
            "Cache {Operation} serialization failed for key {CacheKey}. ErrorCode={ErrorCode}",
            operation,
            key,
            CacheErrors.SerializationFailed);
    }

    protected Task<T> GetOrSetCoreAsync<T>(
        ICacheService cacheService,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken) =>
        GetOrSetCoreAsyncInternal(cacheService, key, factory, expiration, cancellationToken);

    private static async Task<T> GetOrSetCoreAsyncInternal<T>(
        ICacheService cacheService,
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        if (await cacheService.ExistsAsync(key, cancellationToken))
        {
            return (await cacheService.GetAsync<T>(key, cancellationToken))!;
        }

        var value = await factory(cancellationToken);
        if (value is null)
        {
            return value!;
        }

        await cacheService.SetAsync(key, value, expiration, cancellationToken);
        return value;
    }
}
