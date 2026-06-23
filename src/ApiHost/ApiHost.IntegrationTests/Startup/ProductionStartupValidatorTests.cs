using ApiHost.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ApiHost.IntegrationTests.Startup;

public sealed class ProductionStartupValidatorTests
{
    [Fact]
    public void Validate_InProduction_WithMissingConnectionString_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("DefaultConnection", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithWeakConnectionPassword_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Host=db;Database=app;Username=u;Password=postgres"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("password", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithWildcardCors_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = "*" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("Cors", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithSwaggerEnabled_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?> { ["Swagger:Enabled"] = "true" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("Swagger", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithUnprotectedHangfireDashboard_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["BackgroundJobs:Dashboard:Enabled"] = "true",
                ["BackgroundJobs:Dashboard:BasicAuth:Enabled"] = "false"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("Hangfire", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithDefaultHangfireBasicAuth_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["BackgroundJobs:Dashboard:Enabled"] = "true",
                ["BackgroundJobs:Dashboard:BasicAuth:Enabled"] = "true",
                ["BackgroundJobs:Dashboard:BasicAuth:UserName"] = "hangfire",
                ["BackgroundJobs:Dashboard:BasicAuth:Password"] = "dev_hangfire_password_change_me"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("Hangfire", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithProtectedHangfireDashboard_DoesNotThrow()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["BackgroundJobs:Dashboard:Enabled"] = "true",
                ["BackgroundJobs:Dashboard:BasicAuth:Enabled"] = "true",
                ["BackgroundJobs:Dashboard:BasicAuth:UserName"] = "ops-dashboard",
                ["BackgroundJobs:Dashboard:BasicAuth:Password"] = "StrongHangfireDashboardPassword!"
            });

        var exception = Record.Exception(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_InProduction_WithRedisProviderAndMissingConnectionString_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["Cache:Provider"] = "Redis",
                ["Cache:Redis:ConnectionString"] = ""
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("Redis", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InProduction_WithWeakAdminSeedPassword_Throws()
    {
        var configuration = BuildValidProductionConfiguration(
            overrides: new Dictionary<string, string?>
            {
                ["AdminSeed:Enabled"] = "true",
                ["AdminSeed:Password"] = "Admin@123456"
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Production)));

        Assert.Contains("AdminSeed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_InDevelopment_SkipsValidation()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "",
            ["Swagger:Enabled"] = "true"
        });

        var exception = Record.Exception(() =>
            ProductionStartupValidator.Validate(configuration, CreateEnvironment(Environments.Development)));

        Assert.Null(exception);
    }

    private static IConfiguration BuildValidProductionConfiguration(
        Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Host=db;Database=app;Username=u;Password=SecureProductionPassword123!",
            ["Jwt:Secret"] = new string('a', 64),
            ["RefreshToken:Secret"] = new string('b', 64),
            ["AllowedHosts"] = "api.example.com",
            ["FileStorage:MaxFileSizeMb"] = "20",
            ["FileStorage:Local:RootPath"] = "/data/uploads",
            ["AdminSeed:Enabled"] = "false",
            ["Cache:Provider"] = "Memory"
        };

        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return BuildConfiguration(values);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

    private static IHostEnvironment CreateEnvironment(string environmentName) =>
        new TestHostEnvironment(environmentName);

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
