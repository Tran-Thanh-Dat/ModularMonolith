using BuildingBlocks.Application.Pagination;
using MasterData.Domain.Enums;

namespace MasterData.Application.Dtos;

public sealed class MasterDataGroupDetailResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Description { get; init; }
    public MasterDataScope Scope { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class MasterDataGroupListItemResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public MasterDataScope Scope { get; init; }
    public Guid? TenantId { get; init; }
    public Guid? OrganizationId { get; init; }
    public bool IsSystem { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreateMasterDataGroupResponse
{
    public Guid Id { get; init; }
}

public sealed class MasterDataItemDetailResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Value { get; init; }
    public string? Description { get; init; }
    public Guid? ParentItemId { get; init; }
    public bool IsSystem { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public string? Metadata { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}

public sealed class MasterDataItemListItemResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string GroupCode { get; init; } = default!;
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public bool IsSystem { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
}

public sealed class CreateMasterDataItemResponse
{
    public Guid Id { get; init; }
}

public sealed class LookupItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = default!;
    public string Name { get; init; } = default!;
    public string? Value { get; init; }
    public string? Description { get; init; }
    public Guid? ParentItemId { get; init; }
    public bool IsDefault { get; init; }
    public int SortOrder { get; init; }
    public string? Metadata { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
}

public sealed class LookupGroupDto
{
    public string GroupCode { get; init; } = default!;
    public string GroupName { get; init; } = default!;
    public MasterDataScope Scope { get; init; }
    public IReadOnlyList<LookupItemDto> Items { get; init; } = [];
}

public sealed class BatchLookupResponse
{
    public IReadOnlyDictionary<string, IReadOnlyList<LookupItemDto>> Groups { get; init; } =
        new Dictionary<string, IReadOnlyList<LookupItemDto>>();
}

public sealed record MasterDataGroupListQuery(
    string? Keyword,
    string? Code,
    MasterDataScope? Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    bool? IsSystem,
    bool? IsActive,
    int PageIndex,
    int PageSize);

public sealed record MasterDataItemListQuery(
    Guid? GroupId,
    string? GroupCode,
    string? Keyword,
    string? Code,
    Guid? ParentItemId,
    bool? IsSystem,
    bool? IsDefault,
    bool? IsActive,
    DateTimeOffset? EffectiveAt,
    int PageIndex,
    int PageSize);

public sealed record LookupQuery(
    string? GroupCode,
    IReadOnlyList<string>? GroupCodes,
    MasterDataScope Scope,
    Guid? TenantId,
    Guid? OrganizationId,
    bool IncludeInactive,
    DateTimeOffset? EffectiveAt,
    bool IncludeMetadata);
