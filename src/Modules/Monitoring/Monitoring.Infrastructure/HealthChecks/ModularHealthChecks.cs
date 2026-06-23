using BackgroundJobs.Application.Options;
using BuildingBlocks.Application.Caching;
using Files.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Notifications.Application.Options;

namespace Monitoring.Infrastructure.HealthChecks;

internal sealed class CacheProviderHealthCheck : IHealthCheck
{
    private readonly CacheOptions _cacheOptions;

    public CacheProviderHealthCheck(IOptions<CacheOptions> cacheOptions)
    {
        _cacheOptions = cacheOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = _cacheOptions.Provider?.Trim() ?? CacheProviders.Memory;

        return provider.ToUpperInvariant() switch
        {
            var p when p == CacheProviders.None.ToUpperInvariant() =>
                Task.FromResult(HealthCheckResult.Healthy("Cache disabled (Provider=None).")),

            var p when p == CacheProviders.Memory.ToUpperInvariant() =>
                Task.FromResult(HealthCheckResult.Degraded(
                    "In-memory cache is active. Safe for single-instance development only; use Redis for multi-instance production.")),

            _ => Task.FromResult(HealthCheckResult.Degraded("Cache provider status unknown."))
        };
    }
}

internal sealed class HangfireHealthCheck : IHealthCheck
{
    private readonly BackgroundJobsOptions _backgroundJobsOptions;

    public HangfireHealthCheck(IOptions<BackgroundJobsOptions> backgroundJobsOptions)
    {
        _backgroundJobsOptions = backgroundJobsOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_backgroundJobsOptions.Enabled)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Background jobs disabled."));
        }

        try
        {
            var storage = Hangfire.JobStorage.Current;
            if (storage is null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Hangfire storage is not configured."));
            }

            using var connection = storage.GetConnection();
            var servers = storage.GetMonitoringApi().Servers();
            var message = servers.Count > 0
                ? "Hangfire storage is reachable with active servers."
                : "Hangfire storage is reachable.";

            _ = connection;
            return Task.FromResult(HealthCheckResult.Healthy(message));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Hangfire storage is unavailable.",
                ex));
        }
    }
}

internal sealed class SmtpConfigurationHealthCheck : IHealthCheck
{
    private readonly EmailOptions _emailOptions;

    public SmtpConfigurationHealthCheck(IOptions<EmailOptions> emailOptions)
    {
        _emailOptions = emailOptions.Value;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_emailOptions.Provider))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Email provider is not configured."));
        }

        if (string.IsNullOrWhiteSpace(_emailOptions.FromAddress))
        {
            return Task.FromResult(HealthCheckResult.Degraded("Email FromAddress is not configured."));
        }

        if (!string.Equals(_emailOptions.Provider, "Smtp", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(HealthCheckResult.Healthy($"Email provider '{_emailOptions.Provider}' configured."));
        }

        if (string.IsNullOrWhiteSpace(_emailOptions.Smtp.Host) || _emailOptions.Smtp.Port <= 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded("SMTP host or port is not configured."));
        }

        var testModeMessage = _emailOptions.TestMode.Enabled
            ? " Test mode enabled."
            : string.Empty;

        return Task.FromResult(HealthCheckResult.Healthy(
            $"SMTP configuration is present.{testModeMessage}"));
    }
}

internal sealed class PostgreSqlConfigurationHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Unhealthy("DefaultConnection is not configured."));
}

internal sealed class RedisConfigurationHealthCheck : IHealthCheck
{
    private readonly CacheOptions _cacheOptions;
    private readonly IHostEnvironment _hostEnvironment;

    public RedisConfigurationHealthCheck(
        IOptions<CacheOptions> cacheOptions,
        IHostEnvironment hostEnvironment)
    {
        _cacheOptions = cacheOptions.Value;
        _hostEnvironment = hostEnvironment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cacheOptions.Redis.ConnectionString))
        {
            return Task.FromResult(HealthCheckResult.Healthy("Redis connection string is configured."));
        }

        if (_cacheOptions.FallbackToMemoryInDevelopment && _hostEnvironment.IsDevelopment())
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                "Redis connection string is not configured. Development memory fallback may be active."));
        }

        return Task.FromResult(HealthCheckResult.Unhealthy(
            "Redis connection string is not configured."));
    }
}

internal sealed class FileStorageHealthCheck : IHealthCheck
{
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly IHostEnvironment _hostEnvironment;

    public FileStorageHealthCheck(
        IOptions<FileStorageOptions> fileStorageOptions,
        IHostEnvironment hostEnvironment)
    {
        _fileStorageOptions = fileStorageOptions.Value;
        _hostEnvironment = hostEnvironment;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(_fileStorageOptions.Provider, "Local", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(HealthCheckResult.Healthy(
                $"File storage provider '{_fileStorageOptions.Provider}' configured."));
        }

        try
        {
            var rootPath = ResolveRootPath(_fileStorageOptions.Local.RootPath);
            Directory.CreateDirectory(rootPath);

            var probeFile = Path.Combine(rootPath, $".healthcheck-{Guid.NewGuid():N}.tmp");
            File.WriteAllText(probeFile, "ok");
            File.Delete(probeFile);

            return Task.FromResult(HealthCheckResult.Healthy("Local file storage is writable."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Local file storage is not accessible.",
                ex));
        }
    }

    private string ResolveRootPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return configuredPath;
        }

        return Path.GetFullPath(Path.Combine(_hostEnvironment.ContentRootPath, configuredPath));
    }
}
