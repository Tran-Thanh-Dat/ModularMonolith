using Organizations.Domain.Enums;

namespace Organizations.Api.Contracts;

public sealed class CreateTenantRequest
{
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateTenantRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public sealed class CreateOrganizationRequest
{
    public Guid TenantId { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OrganizationType Type { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateOrganizationRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OrganizationType Type { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
    public Guid? ParentOrganizationId { get; init; }
}

public sealed class AssignOrganizationUserRequest
{
    public Guid TenantId { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public bool IsDefault { get; init; }
}

public sealed class CreateWorkspaceRequest
{
    public Guid TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateWorkspaceRequest
{
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public Guid? OrganizationId { get; init; }
    public string? Metadata { get; init; }
}

public sealed class AssignWorkspaceUserRequest
{
    public Guid TenantId { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid UserId { get; init; }
}
