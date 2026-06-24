using AuthorizationPolicies.Domain.Enums;

namespace AuthorizationPolicies.Application.Models;

public interface IAuthorizationResourceContext
{
    string? ResourceType { get; }

    Guid? ResourceId { get; }

    Guid? TenantId { get; }

    Guid? OrganizationId { get; }

    Guid? WorkspaceId { get; }

    Guid? OwnerUserId { get; }

    IReadOnlyCollection<Guid> AssignedUserIds { get; }

    Guid? CreatedBy { get; }

    string? Metadata { get; }
}

public sealed class AuthorizationResourceContext : IAuthorizationResourceContext
{
    public string? ResourceType { get; init; }

    public Guid? ResourceId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public Guid? WorkspaceId { get; init; }

    public Guid? OwnerUserId { get; init; }

    public IReadOnlyCollection<Guid> AssignedUserIds { get; init; } = [];

    public Guid? CreatedBy { get; init; }

    public string? Metadata { get; init; }
}

public sealed class CurrentUserPermissionContext
{
    public Guid UserId { get; init; }

    public IReadOnlyCollection<Guid> RoleIds { get; init; } = [];

    public IReadOnlyCollection<string> PermissionCodes { get; init; } = [];

    public IReadOnlyCollection<Guid> TenantIds { get; init; } = [];

    public IReadOnlyCollection<Guid> OrganizationIds { get; init; } = [];

    public IReadOnlyCollection<Guid> WorkspaceIds { get; init; } = [];

    public Guid? DefaultTenantId { get; init; }

    public Guid? DefaultOrganizationId { get; init; }

    public bool IsAuthenticated { get; init; }
}

public sealed class AuthorizationDecision
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

public sealed class EffectivePolicyInfo
{
    public Guid PolicyId { get; init; }

    public string PolicyCode { get; init; } = default!;

    public string PermissionCode { get; init; } = default!;

    public AuthorizationScope Scope { get; init; }

    public AuthorizationEffect Effect { get; init; }

    public int Priority { get; init; }

    public string Source { get; init; } = default!;
}

public sealed class AuthorizationExplanation
{
    public AuthorizationDecision Decision { get; init; } = default!;

    public IReadOnlyList<string> Steps { get; init; } = [];

    public IReadOnlyList<EffectivePolicyInfo> MatchedPolicies { get; init; } = [];
}
