using BuildingBlocks.Web.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ApiHost.Startup;

public static class ProductionStartupValidator
{
    private static readonly string[] PlaceholderMarkers =
    [
        "CHANGE_ME",
        "dev_jwt_secret",
        "dev_refresh_secret",
        "dev_postgres_password",
        "dev_hangfire_password",
        "YOUR_PASSWORD",
        "YOUR_LONG_RANDOM"
    ];

    private static readonly HashSet<string> WeakPasswords = new(StringComparer.OrdinalIgnoreCase)
    {
        "postgres",
        "password",
        "admin",
        "root",
        "123456",
        "CHANGE_ME",
        "Admin@123456"
    };

    private static readonly HashSet<string> WeakHangfireUserNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "hangfire",
        "admin"
    };

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsProduction())
        {
            return;
        }

        ValidateConnectionString(configuration);
        ValidateJwtSecrets(configuration);
        ValidateCors(configuration);
        ValidateHangfireDashboard(configuration);
        ValidateSwaggerDisabled(environment, configuration);
        ValidateAllowedHosts(configuration);
        ValidateFileStorage(configuration);
        ValidateAdminSeed(configuration);
        ValidateRedis(configuration);
    }

    private static void ValidateConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required in Production.");
        }

        if (ContainsPlaceholder(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must not use placeholder values in Production.");
        }

        var password = ExtractPasswordFromConnectionString(connectionString);

        if (string.IsNullOrWhiteSpace(password) ||
            WeakPasswords.Contains(password) ||
            password.Length < 12)
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must use a strong non-default password in Production.");
        }
    }

    private static void ValidateJwtSecrets(IConfiguration configuration)
    {
        ValidateSecret(configuration, "Jwt:Secret", "Jwt:Secret");
        ValidateSecret(configuration, "RefreshToken:Secret", "RefreshToken:Secret");
    }

    private static void ValidateSecret(IConfiguration configuration, string key, string displayName)
    {
        var secret = configuration[key];

        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
        {
            throw new InvalidOperationException($"{displayName} must be at least 32 characters in Production.");
        }

        if (ContainsPlaceholder(secret))
        {
            throw new InvalidOperationException($"{displayName} must not use placeholder values in Production.");
        }
    }

    private static void ValidateCors(IConfiguration configuration)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        if (corsOptions.AllowedOrigins.Any(origin =>
                string.Equals(origin, "*", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must not contain '*' in Production.");
        }
    }

    private static void ValidateHangfireDashboard(IConfiguration configuration)
    {
        var dashboardEnabled = configuration.GetValue("BackgroundJobs:Dashboard:Enabled", false);
        if (!dashboardEnabled)
        {
            return;
        }

        var basicAuthEnabled = configuration.GetValue("BackgroundJobs:Dashboard:BasicAuth:Enabled", false);
        var userName = configuration["BackgroundJobs:Dashboard:BasicAuth:UserName"];
        var password = configuration["BackgroundJobs:Dashboard:BasicAuth:Password"];

        if (!basicAuthEnabled ||
            string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password) ||
            ContainsPlaceholder(password) ||
            WeakPasswords.Contains(password) ||
            (WeakHangfireUserNames.Contains(userName) && password.Length < 16))
        {
            throw new InvalidOperationException(
                "Hangfire dashboard must be disabled or protected with non-default Basic Auth credentials in Production.");
        }
    }

    private static void ValidateSwaggerDisabled(IHostEnvironment environment, IConfiguration configuration)
    {
        var swaggerEnabled = configuration.GetValue("Swagger:Enabled", false);

        if (swaggerEnabled && environment.IsProduction())
        {
            throw new InvalidOperationException(
                "Swagger must remain disabled in Production unless explicitly protected by a reverse proxy.");
        }
    }

    private static void ValidateAllowedHosts(IConfiguration configuration)
    {
        var allowedHosts = configuration["AllowedHosts"];

        if (string.IsNullOrWhiteSpace(allowedHosts) ||
            string.Equals(allowedHosts, "*", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "AllowedHosts must be configured to explicit host names in Production.");
        }
    }

    private static void ValidateFileStorage(IConfiguration configuration)
    {
        var maxFileSizeMb = configuration.GetValue("FileStorage:MaxFileSizeMb", 0);

        if (maxFileSizeMb is <= 0 or > 200)
        {
            throw new InvalidOperationException(
                "FileStorage:MaxFileSizeMb must be between 1 and 200 in Production.");
        }

        var rootPath = configuration["FileStorage:Local:RootPath"];

        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new InvalidOperationException("FileStorage:Local:RootPath must be configured in Production.");
        }
    }

    private static void ValidateAdminSeed(IConfiguration configuration)
    {
        var adminSeedEnabled = configuration.GetValue("AdminSeed:Enabled", true);
        var migrationsOnStartup = configuration.GetValue("Database:ApplyMigrationsOnStartup", false);
        var password = configuration["AdminSeed:Password"];

        if (!string.IsNullOrWhiteSpace(password) &&
            (ContainsPlaceholder(password) || WeakPasswords.Contains(password)))
        {
            throw new InvalidOperationException(
                "AdminSeed:Password must not use placeholder or default values in Production.");
        }

        if (!adminSeedEnabled && !migrationsOnStartup)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || WeakPasswords.Contains(password))
        {
            throw new InvalidOperationException(
                "AdminSeed requires a strong password from environment/secret store when enabled in Production.");
        }
    }

    private static void ValidateRedis(IConfiguration configuration)
    {
        var provider = configuration["Cache:Provider"];

        if (!string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var connectionString = configuration["Cache:Redis:ConnectionString"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            ContainsPlaceholder(connectionString) ||
            string.Equals(connectionString, "localhost:6379", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Cache:Redis:ConnectionString must be configured when Cache:Provider is Redis in Production.");
        }
    }

    private static string? ExtractPasswordFromConnectionString(string connectionString)
    {
        foreach (var segment in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = segment[..separatorIndex].Trim();
            if (key.Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                key.Equals("Pwd", StringComparison.OrdinalIgnoreCase))
            {
                return segment[(separatorIndex + 1)..].Trim();
            }
        }

        return null;
    }

    private static bool ContainsPlaceholder(string value) =>
        PlaceholderMarkers.Any(marker =>
            value.Contains(marker, StringComparison.OrdinalIgnoreCase));
}
