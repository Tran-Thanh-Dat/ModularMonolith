using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Infrastructure.Http;

public static class HttpContextTraceIdResolver
{
    private const string HttpContextItemKey = "CorrelationId";
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public static string Resolve(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemKey, out var value) &&
            value is string correlationId &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId;
        }

        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var headerValue))
        {
            var headerCorrelationId = headerValue.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(headerCorrelationId))
            {
                return headerCorrelationId;
            }
        }

        return context.TraceIdentifier;
    }
}
