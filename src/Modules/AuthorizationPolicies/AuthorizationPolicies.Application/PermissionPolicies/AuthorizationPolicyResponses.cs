using AuthorizationPolicies.Domain.Enums;

namespace AuthorizationPolicies.Application.PermissionPolicies;

public sealed class PermissionPolicyListItemResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string PermissionCode { get; init; } = default!;

    public string ModuleCode { get; init; } = default!;

    public string ResourceType { get; init; } = default!;

    public string Action { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public AuthorizationEffect Effect { get; init; }

    public int Priority { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class PermissionPolicyDetailResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public string PermissionCode { get; init; } = default!;

    public string ModuleCode { get; init; } = default!;

    public string ResourceType { get; init; } = default!;

    public string Action { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public AuthorizationEffect Effect { get; init; }

    public int Priority { get; init; }

    public bool IsActive { get; init; }

    public string? Conditions { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreatePermissionPolicyResponse
{
    public Guid Id { get; init; }
}

public sealed class RolePermissionPolicyListItemResponse
{
    public Guid Id { get; init; }

    public Guid RoleId { get; init; }

    public Guid PermissionPolicyId { get; init; }

    public string PermissionPolicyCode { get; init; } = default!;

    public string PermissionCode { get; init; } = default!;

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AssignRolePermissionPolicyResponse
{
    public Guid Id { get; init; }
}

public sealed class UserPermissionPolicyOverrideListItemResponse
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid PermissionPolicyId { get; init; }

    public string PermissionPolicyCode { get; init; } = default!;

    public string PermissionCode { get; init; } = default!;

    public AuthorizationEffect Effect { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public string? Reason { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateUserPermissionPolicyOverrideResponse
{
    public Guid Id { get; init; }
}

public sealed class AuthorizationMatrixEntryListItemResponse
{
    public Guid Id { get; init; }

    public string ModuleCode { get; init; } = default!;

    public string ResourceType { get; init; } = default!;

    public string Action { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public string RequiredPermissionCode { get; init; } = default!;

    public bool IsEnabled { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class AuthorizationMatrixEntryDetailResponse
{
    public Guid Id { get; init; }

    public string ModuleCode { get; init; } = default!;

    public string ResourceType { get; init; } = default!;

    public string Action { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public string RequiredPermissionCode { get; init; } = default!;

    public string? Description { get; init; }

    public string? Metadata { get; init; }

    public bool IsEnabled { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class CreateAuthorizationMatrixEntryResponse
{
    public Guid Id { get; init; }
}

public sealed class AuthorizationResourceContextDto
{
    public string? ResourceType { get; init; }

    public Guid? ResourceId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? WorkspaceId { get; init; }

    public Guid? OwnerUserId { get; init; }

    public IReadOnlyCollection<Guid>? AssignedUserIds { get; init; }

    public Guid? CreatedBy { get; init; }

    public string? Metadata { get; init; }
}

public sealed class AuthorizationDecisionResponse
{
    public bool IsAllowed { get; init; }

    public AuthorizationEffect Effect { get; init; }

    public string Reason { get; init; } = default!;

    public string? MatchedPermissionCode { get; init; }

    public string? MatchedPolicyCode { get; init; }

    public AuthorizationScope? Scope { get; init; }

    public string? ResourceType { get; init; }

    public Guid? ResourceId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? WorkspaceId { get; init; }
}

public sealed class AuthorizationExplanationResponse
{
    public AuthorizationDecisionResponse Decision { get; init; } = default!;

    public IReadOnlyList<string> Steps { get; init; } = [];

    public IReadOnlyList<EffectivePolicyInfoResponse> MatchedPolicies { get; init; } = [];
}

public sealed class EffectivePolicyInfoResponse
{
    public Guid PolicyId { get; init; }

    public string PolicyCode { get; init; } = default!;

    public string PermissionCode { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public AuthorizationEffect Effect { get; init; }

    public int Priority { get; init; }

    public string Source { get; init; } = default!;
}
