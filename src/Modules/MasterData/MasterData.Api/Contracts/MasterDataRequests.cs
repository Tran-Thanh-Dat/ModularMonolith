namespace MasterData.Api.Contracts;

public sealed class CreateMasterDataGroupRequest
{
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public string Scope { get; init; } = default!;
    public Guid? TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateMasterDataGroupRequest
{
    public string? Code { get; init; }
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
}

public sealed class CreateMasterDataItemRequest
{
    public Guid GroupId { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Value { get; init; }
    public string? Description { get; init; }
    public Guid? ParentItemId { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public string? Metadata { get; init; }
}

public sealed class UpdateMasterDataItemRequest
{
    public string? Code { get; init; }
    public string Name { get; init; } = default!;
    public string? Value { get; init; }
    public string? Description { get; init; }
    public Guid? ParentItemId { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public string? Metadata { get; init; }
}

public sealed class BatchLookupRequest
{
    public IReadOnlyList<string> GroupCodes { get; init; } = [];
    public string Scope { get; init; } = "Global";
    public Guid? TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public bool IncludeInactive { get; init; }
    public DateTimeOffset? EffectiveAt { get; init; }
    public bool IncludeMetadata { get; init; } = true;
}
