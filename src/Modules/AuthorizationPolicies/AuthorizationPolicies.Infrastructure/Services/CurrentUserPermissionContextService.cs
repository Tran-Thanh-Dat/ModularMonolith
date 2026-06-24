using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Models;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using Identity.Application.Abstractions;
using Organizations.Application.Abstractions;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class CurrentUserPermissionContextService : ICurrentUserPermissionContextService
{
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IOrganizationUserService _organizationUserService;
    private readonly IWorkspaceUserService _workspaceUserService;
    private readonly ICurrentUserService _currentUserService;

    public CurrentUserPermissionContextService(
        IIdentityUserRepository identityUserRepository,
        IOrganizationUserService organizationUserService,
        IWorkspaceUserService workspaceUserService,
        ICurrentUserService currentUserService)
    {
        _identityUserRepository = identityUserRepository;
        _organizationUserService = organizationUserService;
        _workspaceUserService = workspaceUserService;
        _currentUserService = currentUserService;
    }

    public async Task<CurrentUserPermissionContext> GetContextAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _identityUserRepository.FindActiveByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(UserErrors.NotFound, "User not found.");
        }

        var roleIds = user.Roles.Select(r => r.Id).ToArray();

        var orgMemberships = await _organizationUserService.GetUserOrganizationsAsync(userId, null, cancellationToken);
        var activeOrgMemberships = orgMemberships.Where(m => m.IsActive).ToList();

        var workspacePage = await _workspaceUserService.GetListAsync(null, null, userId, true, 1, 1000, cancellationToken);
        var activeWorkspaceMemberships = workspacePage.Items.Where(m => m.IsActive).ToList();

        var tenantIds = activeOrgMemberships.Select(m => m.TenantId)
            .Concat(activeWorkspaceMemberships.Select(m => m.TenantId))
            .Distinct()
            .ToArray();

        var organizationIds = activeOrgMemberships.Select(m => m.OrganizationId).Distinct().ToArray();
        var workspaceIds = activeWorkspaceMemberships.Select(m => m.WorkspaceId).Distinct().ToArray();

        var defaultOrg = activeOrgMemberships.FirstOrDefault(m => m.IsDefault) ?? activeOrgMemberships.FirstOrDefault();

        IReadOnlyCollection<string> permissionCodes;
        if (_currentUserService.UserId == userId && _currentUserService.IsAuthenticated)
        {
            permissionCodes = _currentUserService.Permissions;
        }
        else
        {
            permissionCodes = user.Roles
                .SelectMany(r => r.Permissions)
                .Select(p => p.Code)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
        }

        return new CurrentUserPermissionContext
        {
            UserId = userId,
            RoleIds = roleIds,
            PermissionCodes = permissionCodes,
            TenantIds = tenantIds,
            OrganizationIds = organizationIds,
            WorkspaceIds = workspaceIds,
            DefaultTenantId = defaultOrg?.TenantId,
            DefaultOrganizationId = defaultOrg?.OrganizationId,
            IsAuthenticated = true
        };
    }
}
