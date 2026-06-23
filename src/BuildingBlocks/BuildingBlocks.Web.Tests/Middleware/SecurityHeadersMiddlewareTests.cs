using BuildingBlocks.Web.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using SecurityHeadersOptions = BuildingBlocks.Web.Options.SecurityHeadersOptions;
using Xunit;

namespace BuildingBlocks.Web.Tests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WhenEnabled_AddsSecurityHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new SecurityHeadersMiddleware(
            _ => Task.CompletedTask,
            Microsoft.Extensions.Options.Options.Create(new SecurityHeadersOptions { Enabled = true }),
            new TestHostEnvironment(Environments.Development));

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("0", context.Response.Headers["X-XSS-Protection"]);
        Assert.Contains("camera=()", context.Response.Headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenDisabled_DoesNotAddHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new SecurityHeadersMiddleware(
            _ => Task.CompletedTask,
            Microsoft.Extensions.Options.Options.Create(new SecurityHeadersOptions { Enabled = false }),
            new TestHostEnvironment(Environments.Production));

        await middleware.InvokeAsync(context);

        Assert.False(context.Response.Headers.ContainsKey("X-Content-Type-Options"));
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
