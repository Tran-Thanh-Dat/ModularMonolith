using Organizations.Domain.Enums;

namespace Organizations.Application.Organizations;

public sealed class OrganizationListItemResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public OrganizationType Type { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class OrganizationDetailResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public OrganizationType Type { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public sealed class OrganizationTreeNodeResponse
{
    public Guid Id { get; init; }
    public Guid? ParentOrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public OrganizationType Type { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public IReadOnlyList<OrganizationTreeNodeResponse> Children { get; init; } = [];
}

public sealed class CreateOrganizationResponse
{
    public Guid Id { get; init; }
}
