using Microsoft.AspNetCore.Http;
using BuildingBlocks.Infrastructure.Http;
using Xunit;

namespace BuildingBlocks.Infrastructure.Tests.Http;

public sealed class HttpContextTraceIdResolverTests
{
    [Fact]
    public void Resolve_PrefersCorrelationIdFromHttpContextItems()
    {
        var context = new DefaultHttpContext();
        context.Items["CorrelationId"] = "abc123";
        context.TraceIdentifier = "aspnet-trace";

        var traceId = HttpContextTraceIdResolver.Resolve(context);

        Assert.Equal("abc123", traceId);
    }

    [Fact]
    public void Resolve_UsesRequestHeaderWhenItemMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "header-id";
        context.TraceIdentifier = "aspnet-trace";

        var traceId = HttpContextTraceIdResolver.Resolve(context);

        Assert.Equal("header-id", traceId);
    }

    [Fact]
    public void Resolve_FallsBackToTraceIdentifier()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "aspnet-trace"
        };

        var traceId = HttpContextTraceIdResolver.Resolve(context);

        Assert.Equal("aspnet-trace", traceId);
    }
}
