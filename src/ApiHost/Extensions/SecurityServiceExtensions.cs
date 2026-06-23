using System.Threading.RateLimiting;
using BuildingBlocks.Web.Middleware;
using BuildingBlocks.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CorsOptions = BuildingBlocks.Web.Options.CorsOptions;

namespace ApiHost.Extensions;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddApiSecurityServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.Configure<SecurityHeadersOptions>(configuration.GetSection(SecurityHeadersOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        AddCorsPolicy(services, configuration, environment);
        AddRateLimiting(services, configuration);
        ConfigureUploadLimits(services, configuration);

        return services;
    }

    public static IApplicationBuilder UseApiSecurityMiddleware(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var environment = app.ApplicationServices.GetRequiredService<IHostEnvironment>();
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        app.UseSecurityHeaders();

        if (ShouldUseCors(environment, corsOptions))
        {
            app.UseCors(CorsOptions.SectionName);
        }

        var rateLimitOptions = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        if (rateLimitOptions.Enabled)
        {
            app.UseRateLimiter();
        }

        return app;
    }

    private static void AddCorsPolicy(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var corsOptions = configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

        if (!ShouldUseCors(environment, corsOptions))
        {
            return;
        }

        services.AddCors(options =>
        {
            options.AddPolicy(CorsOptions.SectionName, policy =>
            {
                policy.WithOrigins(corsOptions.AllowedOrigins)
                    .WithMethods(corsOptions.AllowedMethods)
                    .WithHeaders(corsOptions.AllowedHeaders);

                if (corsOptions.AllowCredentials)
                {
                    policy.AllowCredentials();
                }
            });
        });
    }

    private static bool ShouldUseCors(IHostEnvironment environment, CorsOptions corsOptions)
    {
        if (corsOptions.AllowedOrigins.Length == 0)
        {
            return false;
        }

        if (corsOptions.AllowedOrigins.Any(origin =>
                string.Equals(origin, "*", StringComparison.Ordinal)))
        {
            return environment.IsDevelopment() || environment.IsEnvironment("Docker");
        }

        return true;
    }

    private static void AddRateLimiting(IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        if (!options.Enabled)
        {
            return;
        }

        services.AddRateLimiter(rateLimiterOptions =>
        {
            rateLimiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers["Retry-After"] =
                        ((int)retryAfter.TotalSeconds).ToString();
                }

                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    code = "Common.TooManyRequests",
                    message = "Too many requests. Please try again later."
                }, cancellationToken);
            };

            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                if (IsHealthProbePath(httpContext.Request.Path))
                {
                    return RateLimitPartition.GetNoLimiter("health-probe");
                }

                var path = httpContext.Request.Path.Value ?? string.Empty;

                if (path.Contains("/auth/login", StringComparison.OrdinalIgnoreCase))
                {
                    return CreatePartition(
                        httpContext,
                        options.LoginPermitLimit,
                        options.LoginWindowSeconds,
                        "login");
                }

                if (path.Contains("/auth/refresh-token", StringComparison.OrdinalIgnoreCase))
                {
                    return CreatePartition(
                        httpContext,
                        options.RefreshPermitLimit,
                        options.RefreshWindowSeconds,
                        "refresh");
                }

                if (path.Contains("/files/upload", StringComparison.OrdinalIgnoreCase))
                {
                    return CreatePartition(httpContext, options.FileUploadPermitLimit, options.FileUploadWindowSeconds, "upload");
                }

                if (path.Contains("/background-jobs", StringComparison.OrdinalIgnoreCase))
                {
                    return CreatePartition(httpContext, options.BackgroundJobsPermitLimit, options.BackgroundJobsWindowSeconds, "jobs");
                }

                return CreatePartition(httpContext, options.GeneralPermitLimit, options.GeneralWindowSeconds, "api");
            });
        });
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext httpContext,
        int permitLimit,
        int windowSeconds,
        string suffix) =>
        RateLimitPartition.GetFixedWindowLimiter(
            $"{ResolvePartitionKey(httpContext)}:{suffix}",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(windowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });

    private static string ResolvePartitionKey(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    private static bool IsHealthProbePath(PathString path) =>
        path.StartsWithSegments("/health/live", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWithSegments("/health/ready", StringComparison.OrdinalIgnoreCase);

    private static void ConfigureUploadLimits(IServiceCollection services, IConfiguration configuration)
    {
        var maxFileSizeMb = configuration.GetValue("FileStorage:MaxFileSizeMb", 20);
        var maxBytes = maxFileSizeMb * 1024L * 1024L + (1024 * 1024);

        services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(formOptions =>
        {
            formOptions.MultipartBodyLengthLimit = maxBytes;
        });

        services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(kestrelOptions =>
        {
            kestrelOptions.Limits.MaxRequestBodySize = maxBytes;
        });
    }
}
