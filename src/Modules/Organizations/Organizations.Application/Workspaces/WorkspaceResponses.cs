namespace Organizations.Application.Workspaces;

public sealed class WorkspaceListItemResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class WorkspaceDetailResponse
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public string? Metadata { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public Guid? CreatedBy { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public Guid? UpdatedBy { get; init; }
}

public sealed class CreateWorkspaceResponse
{
    public Guid Id { get; init; }
}
