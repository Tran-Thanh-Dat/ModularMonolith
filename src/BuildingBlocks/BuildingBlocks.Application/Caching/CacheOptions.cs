namespace BuildingBlocks.Application.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public string Provider { get; set; } = CacheProviders.Memory;

    public RedisCacheOptions Redis { get; set; } = new();

    public int DefaultExpirationMinutes { get; set; } = 30;

    public int UserPermissionsExpirationMinutes { get; set; } = 20;

    public string KeyPrefix { get; set; } = "modular-monolith";

    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// When true, Redis connection failure during startup throws in non-development environments.
    /// </summary>
    public bool FailFastOnRedisUnavailableInProduction { get; set; } = true;

    /// <summary>
    /// When true, falls back to in-memory cache if Redis is unavailable in Development.
    /// </summary>
    public bool FallbackToMemoryInDevelopment { get; set; } = true;
}

public sealed class RedisCacheOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";

    public string InstanceName { get; set; } = "ModularMonolith:";
}

public static class CacheProviders
{
    public const string None = "None";

    public const string Memory = "Memory";

    public const string Redis = "Redis";
}
