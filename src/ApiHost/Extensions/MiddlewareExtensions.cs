using ApiHost.Middlewares;
using BuildingBlocks.Application.Monitoring;
using BuildingBlocks.Web.Middleware;
using Microsoft.Extensions.Options;

namespace ApiHost.Extensions;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestResponseLoggingMiddleware>();

    public static IApplicationBuilder UseApiMiddlewares(this IApplicationBuilder app)
    {
        var monitoringOptions = app.ApplicationServices.GetRequiredService<IOptions<MonitoringOptions>>().Value;

        if (monitoringOptions.EnableCorrelationId)
        {
            app.UseCorrelationId();
        }

        app.UseGlobalExceptionHandling();

        if (monitoringOptions.EnableRequestLogging)
        {
            app.UseRequestResponseLogging();
        }

        return app;
    }
}
