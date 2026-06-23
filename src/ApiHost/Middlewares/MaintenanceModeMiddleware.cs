using System.Security.Claims;
using System.Text.Json;
using BuildingBlocks.Web.Responses;
using Identity.Domain.Constants;
using Settings.Application.Abstractions;

namespace ApiHost.Middlewares;

public sealed class MaintenanceModeMiddleware
{
    private readonly RequestDelegate _next;

    public MaintenanceModeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAccessPolicyService accessPolicyService)
    {
        if (IsHealthEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        Settings.Application.AccessPolicy.MaintenancePolicyResponse policy;
        try
        {
            policy = await accessPolicyService.GetMaintenancePolicyAsync(context.RequestAborted);
        }
        catch
        {
            await _next(context);
            return;
        }

        if (!IsMaintenanceActive(policy))
        {
            await _next(context);
            return;
        }

        if (policy.AllowAdminBypass && IsAdminUser(context.User))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse.Fail(
            "Maintenance.ServiceUnavailable",
            policy.Message ?? "Service is temporarily unavailable for maintenance.");

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }

    private static bool IsHealthEndpoint(PathString path) =>
        path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);

    private static bool IsMaintenanceActive(Settings.Application.AccessPolicy.MaintenancePolicyResponse policy)
    {
        if (!policy.Enabled)
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;
        if (policy.StartAt.HasValue && now < policy.StartAt.Value)
        {
            return false;
        }

        if (policy.EndAt.HasValue && now > policy.EndAt.Value)
        {
            return false;
        }

        return true;
    }

    private static bool IsAdminUser(ClaimsPrincipal user)
    {
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole(IdentityConstants.Roles.Admin) ||
               user.IsInRole(IdentityConstants.Roles.SuperAdmin);
    }
}
