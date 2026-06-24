using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using MasterData.Application.Abstractions;
using MasterData.Application.Dtos;
using MasterData.Domain.Entities;
using MasterData.Domain.Enums;
using MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MasterData.Infrastructure.Services;

public sealed class LookupService : ILookupService
{
    private readonly MasterDataUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LookupService(
        MasterDataUnitOfWork unitOfWork,
        ICacheService cacheService,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetByGroupCodeAsync(
        LookupQuery query,
        CancellationToken cancellationToken = default)
    {
        var groupCode = query.GroupCode ?? throw new ArgumentException("GroupCode is required.", nameof(query));
        var cacheKey = CacheKeys.MasterDataLookupGroup(
            query.Scope.ToString(),
            query.TenantId,
            query.OrganizationId,
            groupCode,
            query.IncludeInactive,
            query.IncludeMetadata,
            ResolveEffectiveDateKey(query.EffectiveAt));

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var group = await FindGroupAsync(groupCode, query.Scope, query.TenantId, query.OrganizationId, query.IncludeInactive, ct);
                if (group is null)
                {
                    return Array.Empty<LookupItemDto>();
                }

                return await LoadItemsAsync(group, query.IncludeInactive, query.IncludeMetadata, query.EffectiveAt, ct);
            },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupGroupDto>> GetMultipleAsync(
        LookupQuery query,
        CancellationToken cancellationToken = default)
    {
        var codes = query.GroupCodes ?? [];
        var results = new List<LookupGroupDto>();

        foreach (var code in codes)
        {
            var items = await GetByGroupCodeAsync(
                query with { GroupCode = code, GroupCodes = null },
                cancellationToken);

            var group = await FindGroupAsync(code, query.Scope, query.TenantId, query.OrganizationId, query.IncludeInactive, cancellationToken);
            results.Add(new LookupGroupDto
            {
                GroupCode = MasterDataGroup.NormalizeCode(code),
                GroupName = group?.Name ?? MasterDataGroup.NormalizeCode(code),
                Scope = query.Scope,
                Items = items
            });
        }

        return results;
    }

    public async Task<BatchLookupResponse> GetBatchAsync(
        LookupQuery query,
        CancellationToken cancellationToken = default)
    {
        var codes = query.GroupCodes ?? [];
        var cacheKey = CacheKeys.MasterDataLookupBatch(
            query.Scope.ToString(),
            query.TenantId,
            query.OrganizationId,
            codes,
            query.IncludeInactive,
            query.IncludeMetadata,
            ResolveEffectiveDateKey(query.EffectiveAt));

        var dictionary = await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var result = new Dictionary<string, IReadOnlyList<LookupItemDto>>(StringComparer.OrdinalIgnoreCase);

                foreach (var code in codes)
                {
                    var normalized = MasterDataGroup.NormalizeCode(code);
                    var items = await GetByGroupCodeAsync(
                        query with { GroupCode = normalized, GroupCodes = null },
                        ct);
                    result[normalized] = items;
                }

                return (IReadOnlyDictionary<string, IReadOnlyList<LookupItemDto>>)result;
            },
            cancellationToken: cancellationToken);

        return new BatchLookupResponse { Groups = dictionary };
    }

    private async Task<MasterDataGroup?> FindGroupAsync(
        string groupCode,
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var normalizedCode = MasterDataGroup.NormalizeCode(groupCode);
        var query = _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .Where(g => !g.IsDeleted && g.Code == normalizedCode && g.Scope == scope);

        query = scope switch
        {
            MasterDataScope.Global => query.Where(g => g.TenantId == null && g.OrganizationId == null),
            MasterDataScope.Tenant => query.Where(g => g.TenantId == tenantId && g.OrganizationId == null),
            MasterDataScope.Organization => query.Where(g => g.TenantId == tenantId && g.OrganizationId == organizationId),
            _ => query
        };

        if (!includeInactive)
        {
            query = query.Where(g => g.IsActive);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LookupItemDto>> LoadItemsAsync(
        MasterDataGroup group,
        bool includeInactive,
        bool includeMetadata,
        DateTimeOffset? effectiveAt,
        CancellationToken cancellationToken)
    {
        if (!includeInactive && !group.IsActive)
        {
            return Array.Empty<LookupItemDto>();
        }

        var at = effectiveAt ?? _dateTimeProvider.UtcNow;
        var query = _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .Where(i => i.GroupId == group.Id && !i.IsDeleted);

        if (!includeInactive)
        {
            query = query.Where(i => i.IsActive);
        }

        query = query.Where(i =>
            (!i.EffectiveFrom.HasValue || i.EffectiveFrom <= at) &&
            (!i.EffectiveTo.HasValue || i.EffectiveTo >= at));

        var items = await query
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Code)
            .Select(i => new LookupItemDto
            {
                Id = i.Id,
                Code = i.Code,
                Name = i.Name,
                Value = i.Value,
                Description = i.Description,
                ParentItemId = i.ParentItemId,
                IsDefault = i.IsDefault,
                SortOrder = i.SortOrder,
                Metadata = includeMetadata ? i.Metadata : null,
                EffectiveFrom = i.EffectiveFrom,
                EffectiveTo = i.EffectiveTo
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    private string ResolveEffectiveDateKey(DateTimeOffset? effectiveAt) =>
        (effectiveAt ?? _dateTimeProvider.UtcNow).UtcDateTime.ToString("yyyyMMddHHmm");
}
