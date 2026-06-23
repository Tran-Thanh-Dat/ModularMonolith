using AuditLogs.Application.Abstractions;
using BuildingBlocks.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Infrastructure.Services;

public sealed class AuthorizationFailureActivityLoggingHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (!authorizeResult.Succeeded &&
            authorizeResult.Forbidden &&
            authorizeResult.AuthorizationFailure is not null)
        {
            var permission = ResolveRequiredPermission(context);

            if (!string.IsNullOrWhiteSpace(permission))
            {
                var activityLogService = context.RequestServices.GetRequiredService<IActivityLogService>();
                var currentUser = context.RequestServices.GetRequiredService<BuildingBlocks.Application.Abstractions.ICurrentUserService>();

                await activityLogService.LogAuthorizationFailedAsync(
                    currentUser.UserId,
                    currentUser.UserName,
                    permission);
            }
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static string? ResolveRequiredPermission(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            return null;
        }

        var permissionAttribute = endpoint.Metadata.GetMetadata<HasPermissionAttribute>();
        if (permissionAttribute?.Policy is null ||
            !permissionAttribute.Policy.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var anyPermissionAttribute = endpoint.Metadata.GetMetadata<HasAnyPermissionAttribute>();
            if (anyPermissionAttribute?.Policy is null ||
                !anyPermissionAttribute.Policy.StartsWith(HasAnyPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return anyPermissionAttribute.Policy[HasAnyPermissionAttribute.PolicyPrefix.Length..];
        }

        return permissionAttribute.Policy[HasPermissionAttribute.PolicyPrefix.Length..];
    }
}
