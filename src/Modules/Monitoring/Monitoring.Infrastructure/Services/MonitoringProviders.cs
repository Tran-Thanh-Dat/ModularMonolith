using BackgroundJobs.Application.Options;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Infrastructure.Health;
using Files.Application.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monitoring.Application.Abstractions;
using Monitoring.Application.Monitoring;
using Notifications.Application.Options;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Monitoring.Infrastructure.Services;

internal sealed class HealthDetailsProvider : IHealthDetailsProvider
{
    private readonly HealthCheckService _healthCheckService;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly IOptions<BackgroundJobsOptions> _backgroundJobsOptions;
    private readonly IOptions<FileStorageOptions> _fileStorageOptions;
    private readonly IOptions<EmailOptions> _emailOptions;
    private readonly IHostEnvironment _hostEnvironment;

    public HealthDetailsProvider(
        HealthCheckService healthCheckService,
        IOptions<CacheOptions> cacheOptions,
        IOptions<BackgroundJobsOptions> backgroundJobsOptions,
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<EmailOptions> emailOptions,
        IHostEnvironment hostEnvironment)
    {
        _healthCheckService = healthCheckService;
        _cacheOptions = cacheOptions;
        _backgroundJobsOptions = backgroundJobsOptions;
        _fileStorageOptions = fileStorageOptions;
        _emailOptions = emailOptions;
        _hostEnvironment = hostEnvironment;
    }

    public async Task<HealthDetailsResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);

        return new HealthDetailsResponse
        {
            OverallStatus = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Components = report.Entries
                .Select(entry => new ComponentHealthStatusResponse
                {
                    Name = entry.Key,
                    Status = entry.Value.Status.ToString(),
                    Description = HealthCheckDescriptionSanitizer.SanitizeForAdminResponse(entry.Value.Description),
                    DurationMs = entry.Value.Duration.TotalMilliseconds,
                    Tags = entry.Value.Tags.ToArray()
                })
                .OrderBy(component => component.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            Dependencies = BuildDependencySummary()
        };
    }

    private DependencySummaryResponse BuildDependencySummary()
    {
        var cacheOptions = _cacheOptions.Value;
        var redisConfigured = string.Equals(cacheOptions.Provider, CacheProviders.Redis, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(cacheOptions.Redis.ConnectionString);

        return new DependencySummaryResponse
        {
            CacheProvider = cacheOptions.Provider ?? CacheProviders.Memory,
            RedisConfigured = redisConfigured,
            BackgroundJobsEnabled = _backgroundJobsOptions.Value.Enabled,
            FileStorageProvider = _fileStorageOptions.Value.Provider,
            EmailProvider = _emailOptions.Value.Provider,
            Environment = _hostEnvironment.EnvironmentName,
            ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        };
    }
}

internal sealed class SystemInfoProvider : ISystemInfoProvider
{
    private readonly ApplicationUptime _applicationUptime;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IOptions<CacheOptions> _cacheOptions;
    private readonly IOptions<BackgroundJobsOptions> _backgroundJobsOptions;
    private readonly IOptions<FileStorageOptions> _fileStorageOptions;
    private readonly IOptions<EmailOptions> _emailOptions;

    public SystemInfoProvider(
        ApplicationUptime applicationUptime,
        IHostEnvironment hostEnvironment,
        IOptions<CacheOptions> cacheOptions,
        IOptions<BackgroundJobsOptions> backgroundJobsOptions,
        IOptions<FileStorageOptions> fileStorageOptions,
        IOptions<EmailOptions> emailOptions)
    {
        _applicationUptime = applicationUptime;
        _hostEnvironment = hostEnvironment;
        _cacheOptions = cacheOptions;
        _backgroundJobsOptions = backgroundJobsOptions;
        _fileStorageOptions = fileStorageOptions;
        _emailOptions = emailOptions;
    }

    public Task<SystemInfoResponse> GetAsync(CancellationToken cancellationToken = default)
    {
        var response = new SystemInfoResponse
        {
            ApplicationName = Assembly.GetEntryAssembly()?.GetName().Name ?? "ApiHost",
            Environment = _hostEnvironment.EnvironmentName,
            MachineName = Environment.MachineName,
            OsDescription = RuntimeInformation.OSDescription,
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            Uptime = _applicationUptime.Uptime,
            StartedAtUtc = _applicationUptime.StartedAtUtc,
            CurrentTimeUtc = DateTimeOffset.UtcNow,
            Version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(),
            CacheProvider = _cacheOptions.Value.Provider ?? CacheProviders.Memory,
            BackgroundJobsEnabled = _backgroundJobsOptions.Value.Enabled,
            FileStorageProvider = _fileStorageOptions.Value.Provider,
            EmailProvider = _emailOptions.Value.Provider
        };

        return Task.FromResult(response);
    }
}
