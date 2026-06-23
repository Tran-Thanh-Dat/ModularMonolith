using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ApiHost.IntegrationTests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("IntegrationTesting");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["BackgroundJobs:Enabled"] = "false",
                ["HealthChecks:Enabled"] = "true",
                ["Cache:Provider"] = "Memory"
            });
        });

        builder.ConfigureServices(services =>
        {
            var hangfireHostedServices = services
                .Where(descriptor =>
                    descriptor.ServiceType == typeof(IHostedService)
                    && descriptor.ImplementationType?.FullName?.Contains("Hangfire", StringComparison.Ordinal) == true)
                .ToList();

            foreach (var descriptor in hangfireHostedServices)
            {
                services.Remove(descriptor);
            }
        });
    }
}

[CollectionDefinition(ApiHostCollection.Name)]
public sealed class ApiHostCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "ApiHost";
}
