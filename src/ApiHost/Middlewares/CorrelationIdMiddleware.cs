using BuildingBlocks.Web.Constants;
using Serilog.Context;

namespace ApiHost.Middlewares;

public sealed class CorrelationIdMiddleware
{
    private const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);
        context.Items[HttpContextItemKey] = correlationId;

        if (!context.Response.HasStarted)
        {
            context.Response.Headers[HeaderNames.XCorrelationId] = correlationId;
        }

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                if (!context.Response.HasStarted)
                {
                    context.Response.Headers[HeaderNames.XCorrelationId] = correlationId;
                }
            }
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderNames.XCorrelationId, out var headerValue))
        {
            var existing = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(existing))
            {
                return existing;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
