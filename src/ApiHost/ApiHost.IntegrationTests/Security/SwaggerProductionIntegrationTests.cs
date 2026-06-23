using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ApiHost.IntegrationTests.Security;

public sealed class SwaggerProductionIntegrationTests : IClassFixture<ProductionApiFactory>
{
    private readonly HttpClient _client;

    public SwaggerProductionIntegrationTests(ProductionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSwagger_InProduction_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/swagger/index.html");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class ProductionApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Production);

        builder.UseSetting(
            "ConnectionStrings:DefaultConnection",
            "Host=localhost;Database=prod_test;Username=u;Password=SecureProductionPassword123!");
        builder.UseSetting("Jwt:Secret", new string('a', 64));
        builder.UseSetting("RefreshToken:Secret", new string('b', 64));
        builder.UseSetting("AllowedHosts", "localhost");
        builder.UseSetting("FileStorage:MaxFileSizeMb", "20");
        builder.UseSetting("FileStorage:Local:RootPath", "uploads");
        builder.UseSetting("Cache:Provider", "Memory");
        builder.UseSetting("Swagger:Enabled", "false");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
        builder.UseSetting("Database:ApplyMigrationsOnStartup", "false");
        builder.UseSetting("AdminSeed:Enabled", "false");
        builder.UseSetting("HealthChecks:Enabled", "true");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=localhost;Database=prod_test;Username=u;Password=SecureProductionPassword123!",
                ["Jwt:Secret"] = new string('a', 64),
                ["RefreshToken:Secret"] = new string('b', 64),
                ["AllowedHosts"] = "localhost",
                ["FileStorage:MaxFileSizeMb"] = "20",
                ["FileStorage:Local:RootPath"] = "uploads",
                ["Cache:Provider"] = "Memory",
                ["Swagger:Enabled"] = "false",
                ["BackgroundJobs:Enabled"] = "false",
                ["Database:ApplyMigrationsOnStartup"] = "false",
                ["AdminSeed:Enabled"] = "false",
                ["HealthChecks:Enabled"] = "true"
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
