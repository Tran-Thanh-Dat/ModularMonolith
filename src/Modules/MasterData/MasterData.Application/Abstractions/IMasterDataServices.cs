using BuildingBlocks.Application.Pagination;
using MasterData.Application.Dtos;
using MasterData.Domain.Enums;

namespace MasterData.Application.Abstractions;

public interface IMasterDataGroupService
{
    Task<Guid> CreateAsync(
        string code,
        string name,
        string? description,
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        int sortOrder,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string? code,
        string name,
        string? description,
        int sortOrder,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MasterDataGroupDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<MasterDataGroupListItemResponse>> GetListAsync(MasterDataGroupListQuery query, CancellationToken cancellationToken = default);
}

public interface IMasterDataItemService
{
    Task<Guid> CreateAsync(
        Guid groupId,
        string code,
        string name,
        string? value,
        string? description,
        Guid? parentItemId,
        bool isDefault,
        int sortOrder,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string? code,
        string name,
        string? value,
        string? description,
        Guid? parentItemId,
        int sortOrder,
        DateTimeOffset? effectiveFrom,
        DateTimeOffset? effectiveTo,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MasterDataItemDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<MasterDataItemListItemResponse>> GetListAsync(MasterDataItemListQuery query, CancellationToken cancellationToken = default);
}

public interface ILookupService
{
    Task<IReadOnlyList<LookupItemDto>> GetByGroupCodeAsync(LookupQuery query, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LookupGroupDto>> GetMultipleAsync(LookupQuery query, CancellationToken cancellationToken = default);
    Task<BatchLookupResponse> GetBatchAsync(LookupQuery query, CancellationToken cancellationToken = default);
}

public interface IMasterDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
