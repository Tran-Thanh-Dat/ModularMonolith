using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monitoring.Infrastructure.HealthChecks;
using Xunit;

namespace Monitoring.Infrastructure.Tests.Health;

public sealed class RedisConfigurationHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthyWhenRedisConnectionStringMissingInProduction()
    {
        var check = CreateCheck(
            new CacheOptions
            {
                Provider = CacheProviders.Redis,
                Redis = new RedisCacheOptions { ConnectionString = string.Empty },
                FallbackToMemoryInDevelopment = false
            },
            isDevelopment: false);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not configured", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegradedWhenRedisConnectionStringMissingInDevelopmentWithFallback()
    {
        var check = CreateCheck(
            new CacheOptions
            {
                Provider = CacheProviders.Redis,
                Redis = new RedisCacheOptions { ConnectionString = string.Empty },
                FallbackToMemoryInDevelopment = true
            },
            isDevelopment: true);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Degraded, result.Status);
    }

    private static RedisConfigurationHealthCheck CreateCheck(CacheOptions options, bool isDevelopment)
    {
        var hostEnvironment = new TestHostEnvironment(isDevelopment);
        return new RedisConfigurationHealthCheck(Options.Create(options), hostEnvironment);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(bool isDevelopment)
        {
            EnvironmentName = isDevelopment
                ? Environments.Development
                : Environments.Production;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
