namespace Monitoring.Application.Monitoring;

public sealed class ComponentHealthStatusResponse
{
    public required string Name { get; init; }

    public required string Status { get; init; }

    public string? Description { get; init; }

    public double DurationMs { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = [];
}

public sealed class DependencySummaryResponse
{
    public required string CacheProvider { get; init; }

    public bool RedisConfigured { get; init; }

    public bool BackgroundJobsEnabled { get; init; }

    public required string FileStorageProvider { get; init; }

    public required string EmailProvider { get; init; }

    public required string Environment { get; init; }

    public string? ApplicationVersion { get; init; }
}

public sealed class HealthDetailsResponse
{
    public required string OverallStatus { get; init; }

    public double TotalDurationMs { get; init; }

    public IReadOnlyCollection<ComponentHealthStatusResponse> Components { get; init; } = [];

    public required DependencySummaryResponse Dependencies { get; init; }
}

public sealed class SystemInfoResponse
{
    public required string ApplicationName { get; init; }

    public required string Environment { get; init; }

    public required string MachineName { get; init; }

    public required string OsDescription { get; init; }

    public required string ProcessArchitecture { get; init; }

    public required string FrameworkDescription { get; init; }

    public required TimeSpan Uptime { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public required DateTimeOffset CurrentTimeUtc { get; init; }

    public string? Version { get; init; }

    public required string CacheProvider { get; init; }

    public bool BackgroundJobsEnabled { get; init; }

    public required string FileStorageProvider { get; init; }

    public required string EmailProvider { get; init; }
}
