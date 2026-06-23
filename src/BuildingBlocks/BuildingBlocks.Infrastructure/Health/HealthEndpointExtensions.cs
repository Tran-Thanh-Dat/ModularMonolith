using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Infrastructure.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Infrastructure.Health;

public static class HealthEndpointExtensions
{
    public static WebApplication MapModularHealthChecks(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<HealthChecksOptions>>().Value;
        if (!options.Enabled)
        {
            return app;
        }

        var tagOptions = options.Tags;
        var liveTags = HealthCheckTagMatcher.ResolveLiveTags(tagOptions);
        var readyMatchTags = HealthCheckTagMatcher.ResolveReadyMatchTags(tagOptions);
        var dependencyTags = HealthCheckTagMatcher.ResolveDependencyTags(tagOptions);

        ValidateReadinessConfiguration(app, readyMatchTags, dependencyTags);

        var responseWriter = HealthCheckJsonResponseWriter.WriteAsync;

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = registration => HealthCheckTagMatcher.MatchesAnyTag(registration.Tags, liveTags),
            ResponseWriter = responseWriter
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => HealthCheckTagMatcher.MatchesAnyTag(registration.Tags, readyMatchTags),
            ResponseWriter = responseWriter
        });

        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = responseWriter
        });

        return app;
    }

    private static void ValidateReadinessConfiguration(
        WebApplication app,
        string[] readyMatchTags,
        string[] dependencyTags)
    {
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("HealthChecks");
        var summary = app.Services.GetService<HealthCheckRegistrationSummary>();

        if (summary is null)
        {
            return;
        }

        if (summary.DependencyCheckCount == 0)
        {
            logger.LogWarning(
                "Health checks are enabled but no dependency checks were registered. /health/ready will not validate external dependencies.");
            return;
        }

        var readyMatchesDependencyTags = readyMatchTags.Any(readyTag =>
            dependencyTags.Contains(readyTag, StringComparer.OrdinalIgnoreCase));

        if (!readyMatchesDependencyTags)
        {
            logger.LogWarning(
                "Health check tag configuration mismatch: /health/ready match tags [{ReadyMatchTags}] do not overlap dependency tags [{DependencyTags}]. Readiness may return Healthy with no dependency entries.",
                string.Join(", ", readyMatchTags),
                string.Join(", ", dependencyTags));
        }
    }
}
