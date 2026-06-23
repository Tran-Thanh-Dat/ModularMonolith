using BuildingBlocks.Web.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        IOptions<SecurityHeadersOptions> options,
        IHostEnvironment environment)
    {
        _next = next;
        _options = options.Value;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (_options.Enabled)
        {
            ApplyHeaders(context);
        }

        await _next(context);
    }

    private void ApplyHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["X-XSS-Protection"] = "0";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        if (_options.EnableContentSecurityPolicy)
        {
            var isSwaggerPath = context.Request.Path.StartsWithSegments("/swagger");
            var useSwaggerPolicy = isSwaggerPath &&
                (_environment.IsDevelopment() || _environment.IsEnvironment("Docker"));

            headers["Content-Security-Policy"] = useSwaggerPolicy
                ? _options.SwaggerContentSecurityPolicy
                : _options.ContentSecurityPolicy;
        }

        if (_options.EnableStrictTransportSecurity &&
            context.Request.IsHttps)
        {
            var hstsValue = $"max-age={_options.StrictTransportSecurityMaxAgeSeconds}";

            if (_options.IncludeSubDomainsInStrictTransportSecurity)
            {
                hstsValue += "; includeSubDomains";
            }

            headers["Strict-Transport-Security"] = hstsValue;
        }
    }
}
