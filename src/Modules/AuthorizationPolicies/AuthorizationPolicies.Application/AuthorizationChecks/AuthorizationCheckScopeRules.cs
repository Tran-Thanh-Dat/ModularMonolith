using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Application.Exceptions;

namespace AuthorizationPolicies.Application.AuthorizationChecks;

public static class AuthorizationCheckScopeRules
{
    public static void ValidateResourceContext(AuthorizationScope scope, AuthorizationResourceContextDto context)
    {
        switch (scope)
        {
            case AuthorizationScope.Tenant:
                if (!context.TenantId.HasValue)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingTenantId,
                        "Tenant scope requires ResourceContext.TenantId.");
                }

                break;

            case AuthorizationScope.Organization:
                if (!context.OrganizationId.HasValue)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingOrganizationId,
                        "Organization scope requires ResourceContext.OrganizationId.");
                }

                break;

            case AuthorizationScope.Workspace:
                if (!context.WorkspaceId.HasValue)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingWorkspaceId,
                        "Workspace scope requires ResourceContext.WorkspaceId.");
                }

                break;

            case AuthorizationScope.OwnerOnly:
                if (!context.OwnerUserId.HasValue && !context.CreatedBy.HasValue)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingOwnerContext,
                        "OwnerOnly scope requires ResourceContext.OwnerUserId or CreatedBy.");
                }

                break;

            case AuthorizationScope.AssignedOnly:
                if (context.AssignedUserIds is null || context.AssignedUserIds.Count == 0)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingAssignedUsers,
                        "AssignedOnly scope requires non-empty ResourceContext.AssignedUserIds.");
                }

                break;

            case AuthorizationScope.Self:
                if (!context.ResourceId.HasValue && !context.OwnerUserId.HasValue && !context.CreatedBy.HasValue)
                {
                    throw new BadRequestException(
                        AuthorizationCheckErrors.MissingSelfContext,
                        "Self scope requires ResourceContext.ResourceId, OwnerUserId, or CreatedBy.");
                }

                break;

            case AuthorizationScope.Department:
            case AuthorizationScope.Custom:
                throw new BadRequestException(
                    AuthorizationCheckErrors.ScopeNotSupported,
                    $"Scope {scope} is not supported in this phase.");

            case AuthorizationScope.Global:
                break;

            default:
                throw new BadRequestException(
                    AuthorizationCheckErrors.InvalidScope,
                    "Unknown authorization scope.");
        }
    }
}
