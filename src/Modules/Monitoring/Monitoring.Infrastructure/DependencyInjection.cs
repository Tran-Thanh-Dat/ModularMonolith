using BackgroundJobs.Application.Options;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Infrastructure.Health;
using Files.Application.Options;
using Notifications.Application.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Monitoring.Application.Abstractions;
using Monitoring.Infrastructure.HealthChecks;
using AsyncTasks.Infrastructure.Health;
using Monitoring.Infrastructure.Services;

namespace Monitoring.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMonitoringInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<HealthChecksOptions>(configuration.GetSection(HealthChecksOptions.SectionName));
        services.Configure<MonitoringOptions>(configuration.GetSection(MonitoringOptions.SectionName));
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        services.AddSingleton<ApplicationUptime>();
        services.AddScoped<IHealthDetailsProvider, HealthDetailsProvider>();
        services.AddScoped<ISystemInfoProvider, SystemInfoProvider>();

        AddModularHealthChecks(services, configuration, environment);

        return services;
    }

    private static void AddModularHealthChecks(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var healthOptions = configuration.GetSection(HealthChecksOptions.SectionName).Get<HealthChecksOptions>()
            ?? new HealthChecksOptions();

        if (!healthOptions.Enabled)
        {
            return;
        }

        var tagOptions = healthOptions.Tags;
        var liveTags = HealthCheckTagMatcher.ResolveLiveTags(tagOptions);
        var dependencyTags = HealthCheckTagMatcher.ResolveDependencyTags(tagOptions);

        var healthChecksBuilder = services.AddHealthChecks();
        var dependencyCheckCount = 0;
        var liveCheckCount = 0;

        healthChecksBuilder.AddCheck(
            "self",
            () => HealthCheckResult.Healthy("Application process is running."),
            liveTags.Concat(dependencyTags).Distinct(StringComparer.OrdinalIgnoreCase));
        liveCheckCount++;
        dependencyCheckCount++;

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            healthChecksBuilder.AddNpgSql(
                connectionString,
                name: "postgresql",
                tags: dependencyTags,
                timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
            dependencyCheckCount++;
        }
        else
        {
            healthChecksBuilder.AddCheck<PostgreSqlConfigurationHealthCheck>(
                "postgresql",
                tags: dependencyTags,
                timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
            dependencyCheckCount++;
        }

        var cacheProvider = configuration.GetSection($"{CacheOptions.SectionName}:Provider").Get<string>();
        if (string.Equals(cacheProvider, CacheProviders.Redis, StringComparison.OrdinalIgnoreCase))
        {
            var redisConnection = configuration
                .GetSection($"{CacheOptions.SectionName}:Redis:ConnectionString")
                .Get<string>()?
                .Trim();

            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                healthChecksBuilder.AddRedis(
                    redisConnection,
                    name: "redis-cache",
                    tags: dependencyTags,
                    timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
            }
            else
            {
                healthChecksBuilder.AddCheck<RedisConfigurationHealthCheck>(
                    "redis-cache",
                    tags: dependencyTags,
                    timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
            }

            dependencyCheckCount++;
        }
        else
        {
            healthChecksBuilder.AddCheck<CacheProviderHealthCheck>(
                "cache-provider",
                tags: dependencyTags,
                timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
            dependencyCheckCount++;
        }

        healthChecksBuilder.AddCheck<HangfireHealthCheck>(
            "hangfire",
            tags: dependencyTags,
            timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
        dependencyCheckCount++;

        healthChecksBuilder.AddCheck<SmtpConfigurationHealthCheck>(
            "smtp-config",
            tags: dependencyTags,
            timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
        dependencyCheckCount++;

        healthChecksBuilder.AddCheck<FileStorageHealthCheck>(
            "file-storage",
            tags: dependencyTags,
            timeout: TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds)));
        dependencyCheckCount++;

        if (AsyncTasksHealthCheckExtensions.TryAddRabbitMqHealthCheck(
                healthChecksBuilder,
                configuration,
                dependencyTags,
                TimeSpan.FromSeconds(Math.Max(1, healthOptions.TimeoutSeconds))))
        {
            dependencyCheckCount++;
        }

        services.AddSingleton(new HealthCheckRegistrationSummary
        {
            DependencyCheckCount = dependencyCheckCount,
            LiveCheckCount = liveCheckCount
        });
    }
}
