namespace Organizations.Application.Tenants;

public sealed class TenantListItemResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class TenantDetailResponse
{
    public Guid Id { get; init; }
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

public sealed class CreateTenantResponse
{
    public Guid Id { get; init; }
}
