namespace AuthorizationPolicies.Api.Contracts;

public sealed class CreatePermissionPolicyRequest
{
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string PermissionCode { get; init; } = default!;
    public string ModuleCode { get; init; } = default!;
    public string ResourceType { get; init; } = default!;
    public string Action { get; init; } = default!;
    public int Scope { get; init; }
    public int Effect { get; init; }
    public int Priority { get; init; }
    public string? Conditions { get; init; }
}

public sealed class UpdatePermissionPolicyRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string PermissionCode { get; init; } = default!;
    public string ModuleCode { get; init; } = default!;
    public string ResourceType { get; init; } = default!;
    public string Action { get; init; } = default!;
    public int Scope { get; init; }
    public int Effect { get; init; }
    public int Priority { get; init; }
    public string? Conditions { get; init; }
}

public sealed class AssignRolePermissionPolicyRequest
{
    public Guid RoleId { get; init; }
    public Guid PermissionPolicyId { get; init; }
}

public sealed class CreateUserPermissionPolicyOverrideRequest
{
    public Guid UserId { get; init; }
    public Guid PermissionPolicyId { get; init; }
    public int Effect { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public string? Reason { get; init; }
}

public sealed class CreateAuthorizationMatrixEntryRequest
{
    public string ModuleCode { get; init; } = default!;
    public string ResourceType { get; init; } = default!;
    public string Action { get; init; } = default!;
    public int Scope { get; init; }
    public string RequiredPermissionCode { get; init; } = default!;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateAuthorizationMatrixEntryRequest
{
    public string ModuleCode { get; init; } = default!;
    public string ResourceType { get; init; } = default!;
    public string Action { get; init; } = default!;
    public int Scope { get; init; }
    public string RequiredPermissionCode { get; init; } = default!;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public sealed class EvaluateAuthorizationRequest
{
    public Guid UserId { get; init; }
    public string? PermissionCode { get; init; }
    public AuthorizationResourceContextRequest ResourceContext { get; init; } = new();
    public string? Action { get; init; }
    public string? ResourceType { get; init; }
    public string? ModuleCode { get; init; }
}

public sealed class ExplainAuthorizationRequest
{
    public Guid UserId { get; init; }
    public string? PermissionCode { get; init; }
    public AuthorizationResourceContextRequest ResourceContext { get; init; } = new();
    public string? Action { get; init; }
    public string? ResourceType { get; init; }
    public string? ModuleCode { get; init; }
}

public sealed class AuthorizationResourceContextRequest
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
