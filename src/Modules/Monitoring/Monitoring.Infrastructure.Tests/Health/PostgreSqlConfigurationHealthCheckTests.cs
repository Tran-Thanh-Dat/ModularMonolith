using BuildingBlocks.Application.Caching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Monitoring.Infrastructure.HealthChecks;
using Xunit;

namespace Monitoring.Infrastructure.Tests.Health;

public sealed class PostgreSqlConfigurationHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthyWhenDefaultConnectionMissing()
    {
        var check = new PostgreSqlConfigurationHealthCheck();

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("not configured", result.Description, StringComparison.OrdinalIgnoreCase);
    }
}
