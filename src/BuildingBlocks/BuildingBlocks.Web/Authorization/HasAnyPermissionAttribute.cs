using Microsoft.AspNetCore.Authorization;

namespace BuildingBlocks.Web.Authorization;

public sealed class HasAnyPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "AnyPermission:";

    public HasAnyPermissionAttribute(params string[] permissions)
    {
        Policy = $"{PolicyPrefix}{string.Join(',', permissions)}";
    }
}

public sealed class AnyPermissionRequirement : IAuthorizationRequirement
{
    public AnyPermissionRequirement(IReadOnlyCollection<string> permissions)
    {
        Permissions = permissions;
    }

    public IReadOnlyCollection<string> Permissions { get; }
}

public sealed class AnyPermissionAuthorizationHandler : AuthorizationHandler<AnyPermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AnyPermissionRequirement requirement)
    {
        var hasPermission = requirement.Permissions.Any(requiredPermission =>
            context.User.Claims.Any(claim =>
                claim.Type == "permission" &&
                string.Equals(claim.Value, requiredPermission, StringComparison.OrdinalIgnoreCase)));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
