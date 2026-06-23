using System.Net.Http.Headers;
using System.Text;
using BackgroundJobs.Application.Options;
using BackgroundJobs.Application.Permissions;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Infrastructure.Hangfire;

public sealed class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            return false;
        }

        var options = httpContext.RequestServices
            .GetRequiredService<IOptions<BackgroundJobsOptions>>()
            .Value;

        if (options.Dashboard.BasicAuth.Enabled &&
            TryAuthorizeBasicAuth(httpContext, options.Dashboard.BasicAuth))
        {
            return true;
        }

        if (httpContext.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return httpContext.User.Claims.Any(claim =>
            claim.Type == "permission" &&
            string.Equals(claim.Value, BackgroundJobsPermissionCodes.Dashboard, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryAuthorizeBasicAuth(
        HttpContext httpContext,
        DashboardBasicAuthOptions basicAuth)
    {
        if (string.IsNullOrWhiteSpace(basicAuth.UserName) ||
            string.IsNullOrWhiteSpace(basicAuth.Password))
        {
            return false;
        }

        var authorizationHeader = httpContext.Request.Headers.Authorization.ToString();
        if (!AuthenticationHeaderValue.TryParse(authorizationHeader, out var headerValue) ||
            !string.Equals(headerValue.Scheme, "Basic", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(headerValue.Parameter))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(headerValue.Parameter));
            var separatorIndex = decoded.IndexOf(':');
            if (separatorIndex <= 0)
            {
                return false;
            }

            var userName = decoded[..separatorIndex];
            var password = decoded[(separatorIndex + 1)..];

            return string.Equals(userName, basicAuth.UserName, StringComparison.Ordinal) &&
                   string.Equals(password, basicAuth.Password, StringComparison.Ordinal);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
