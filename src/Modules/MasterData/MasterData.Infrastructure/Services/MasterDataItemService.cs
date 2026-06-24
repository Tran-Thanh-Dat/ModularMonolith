using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Domain.Exceptions;
using MasterData.Application.Abstractions;
using MasterData.Application.Constants;
using MasterData.Application.Dtos;
using MasterData.Domain.Entities;
using MasterData.Domain.Errors;
using MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterData.Infrastructure.Services;

public sealed class MasterDataItemService : IMasterDataItemService
{
    private readonly MasterDataUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<MasterDataItemService> _logger;

    public MasterDataItemService(
        MasterDataUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<MasterDataItemService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
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
        CancellationToken cancellationToken = default)
    {
        var group = await GetActiveGroupForItemMutationAsync(groupId, cancellationToken);
        await ValidateParentItemAsync(groupId, parentItemId, null, cancellationToken);

        var normalizedCode = MasterDataItem.NormalizeCode(code);
        if (await ItemCodeExistsAsync(groupId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                MasterDataItemErrors.CodeAlreadyExists,
                $"Master data item code '{normalizedCode}' already exists in this group.");
        }

        var item = MasterDataItem.Create(
            groupId,
            normalizedCode,
            name,
            value,
            description,
            parentItemId,
            isSystem: false,
            isDefault: false,
            sortOrder,
            effectiveFrom,
            effectiveTo,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<MasterDataItem, Guid>().AddAsync(item, cancellationToken);

        if (isDefault)
        {
            await UnsetExistingDefaultAsync(groupId, item.Id, cancellationToken);
            item.SetDefault(true);
        }

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemCreated, item, group.Code, cancellationToken);
        InvalidateLookupCache();
        return item.Id;
    }

    public async Task UpdateAsync(
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
        CancellationToken cancellationToken = default)
    {
        var item = await GetItemForUpdateAsync(id, cancellationToken);
        var group = await GetGroupForItemAsync(item.GroupId, cancellationToken);

        if (!group.IsActive)
        {
            throw new BadRequestException(MasterDataItemErrors.GroupInactive, "Cannot update item in inactive group.");
        }

        await ValidateParentItemAsync(item.GroupId, parentItemId, item.Id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(code) &&
            !string.Equals(item.Code, MasterDataItem.NormalizeCode(code), StringComparison.Ordinal))
        {
            try
            {
                item.ChangeCode(code);
            }
            catch (DomainException ex)
            {
                throw MapDomainException(ex);
            }

            if (await ItemCodeExistsAsync(item.GroupId, item.Code, item.Id, cancellationToken))
            {
                throw new ConflictException(
                    MasterDataItemErrors.CodeAlreadyExists,
                    $"Master data item code '{item.Code}' already exists in this group.");
            }
        }

        try
        {
            item.Update(name, value, description, parentItemId, sortOrder, effectiveFrom, effectiveTo, metadata, item.Id);
        }
        catch (DomainException ex)
        {
            throw MapDomainException(ex);
        }

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemUpdated, item, group.Code, cancellationToken);
        InvalidateLookupCache();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetItemForUpdateAsync(id, cancellationToken);
        var group = await GetGroupForItemAsync(item.GroupId, cancellationToken);

        if (item.IsSystem)
        {
            throw new BadRequestException(
                MasterDataItemErrors.SystemProtected,
                "System item cannot be deleted.");
        }

        try
        {
            item.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);
        }
        catch (DomainException ex)
        {
            throw MapDomainException(ex);
        }

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemDeleted, item, group.Code, cancellationToken);
        InvalidateLookupCache();
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetItemForUpdateAsync(id, cancellationToken);
        var group = await GetActiveGroupForItemMutationAsync(item.GroupId, cancellationToken);

        try
        {
            item.Activate();
        }
        catch (DomainException)
        {
            throw new ConflictException(
                MasterDataItemErrors.AlreadyActive,
                $"Master data item '{item.Code}' is already active.");
        }

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemActivated, item, group.Code, cancellationToken);
        InvalidateLookupCache();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetItemForUpdateAsync(id, cancellationToken);
        var group = await GetGroupForItemAsync(item.GroupId, cancellationToken);

        try
        {
            item.Deactivate();
        }
        catch (DomainException)
        {
            throw new ConflictException(
                MasterDataItemErrors.AlreadyInactive,
                $"Master data item '{item.Code}' is already inactive.");
        }

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemDeactivated, item, group.Code, cancellationToken);
        InvalidateLookupCache();
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetItemForUpdateAsync(id, cancellationToken);
        var group = await GetActiveGroupForItemMutationAsync(item.GroupId, cancellationToken);

        if (!item.IsActive)
        {
            throw new BadRequestException(
                MasterDataItemErrors.InactiveCannotBeDefault,
                "Inactive item cannot be set as default.");
        }

        await UnsetExistingDefaultAsync(item.GroupId, item.Id, cancellationToken);
        item.SetDefault(true);

        await EnqueueItemActivityAsync(MasterDataActivityTypes.ItemSetDefault, item, group.Code, cancellationToken);
        InvalidateLookupCache();
    }

    public async Task<MasterDataItemDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (item is null)
        {
            return null;
        }

        var groupCode = await _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .Where(g => g.Id == item.GroupId)
            .Select(g => g.Code)
            .FirstOrDefaultAsync(cancellationToken);

        return MapDetail(item, groupCode ?? string.Empty);
    }

    public async Task<PagedResult<MasterDataItemListItemResponse>> GetListAsync(
        MasterDataItemListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = query.PageIndex, PageSize = query.PageSize };
        var dbQuery = _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .Where(i => !i.IsDeleted);

        if (query.GroupId.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.GroupId == query.GroupId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.GroupCode))
        {
            var groupCode = MasterDataGroup.NormalizeCode(query.GroupCode);
            var matchingGroupIds = _unitOfWork.Repository<MasterDataGroup, Guid>()
                .QueryReadOnly()
                .Where(g => !g.IsDeleted && g.Code == groupCode)
                .Select(g => g.Id);

            dbQuery = dbQuery.Where(i => matchingGroupIds.Contains(i.GroupId));
        }

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword.Trim()}%";
            dbQuery = dbQuery.Where(i =>
                EF.Functions.ILike(i.Code, pattern) ||
                EF.Functions.ILike(i.Name, pattern) ||
                EF.Functions.ILike(i.Description ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            var code = MasterDataItem.NormalizeCode(query.Code);
            dbQuery = dbQuery.Where(i => i.Code == code);
        }

        if (query.ParentItemId.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.ParentItemId == query.ParentItemId.Value);
        }

        if (query.IsSystem.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.IsSystem == query.IsSystem.Value);
        }

        if (query.IsDefault.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.IsDefault == query.IsDefault.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(i => i.IsActive == query.IsActive.Value);
        }

        if (query.EffectiveAt.HasValue)
        {
            var at = query.EffectiveAt.Value;
            dbQuery = dbQuery.Where(i =>
                (!i.EffectiveFrom.HasValue || i.EffectiveFrom <= at) &&
                (!i.EffectiveTo.HasValue || i.EffectiveTo >= at));
        }

        var projected = from item in dbQuery
                        join groupEntity in _unitOfWork.Repository<MasterDataGroup, Guid>().QueryReadOnly()
                            on item.GroupId equals groupEntity.Id
                        where !groupEntity.IsDeleted
                        orderby item.SortOrder, item.Code
                        select new MasterDataItemListItemResponse
                        {
                            Id = item.Id,
                            GroupId = item.GroupId,
                            GroupCode = groupEntity.Code,
                            Code = item.Code,
                            Name = item.Name,
                            IsSystem = item.IsSystem,
                            IsDefault = item.IsDefault,
                            IsActive = item.IsActive,
                            SortOrder = item.SortOrder
                        };

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    private async Task<MasterDataGroup> GetActiveGroupForItemMutationAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await GetGroupForItemAsync(groupId, cancellationToken);

        if (!group.IsActive)
        {
            throw new BadRequestException(
                MasterDataItemErrors.GroupInactive,
                "Cannot mutate items in an inactive group.");
        }

        return group;
    }

    private async Task<MasterDataGroup> GetGroupForItemAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsDeleted, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException(
                MasterDataGroupErrors.NotFound,
                $"Master data group with id '{groupId}' was not found.");
        }

        return group;
    }

    private async Task<MasterDataItem> GetItemForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .Query()
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(
                MasterDataItemErrors.NotFound,
                $"Master data item with id '{id}' was not found.");
        }

        return item;
    }

    private async Task<bool> ItemCodeExistsAsync(
        Guid groupId,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .Where(i => !i.IsDeleted && i.GroupId == groupId && i.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(i => i.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task ValidateParentItemAsync(
        Guid groupId,
        Guid? parentItemId,
        Guid? itemId,
        CancellationToken cancellationToken)
    {
        if (!parentItemId.HasValue)
        {
            return;
        }

        if (itemId.HasValue && parentItemId.Value == itemId.Value)
        {
            throw new BadRequestException(
                MasterDataItemErrors.ParentSelfReference,
                "Item cannot be its own parent.");
        }

        var parent = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(i => i.Id == parentItemId.Value && !i.IsDeleted, cancellationToken);

        if (parent is null)
        {
            throw new BadRequestException(
                MasterDataItemErrors.InvalidParent,
                "Parent item was not found.");
        }

        if (parent.GroupId != groupId)
        {
            throw new BadRequestException(
                MasterDataItemErrors.InvalidParent,
                "Parent item must belong to the same group.");
        }

        if (itemId.HasValue)
        {
            await EnsureNoCircularParentAsync(itemId.Value, parentItemId.Value, cancellationToken);
        }
    }

    private async Task EnsureNoCircularParentAsync(
        Guid itemId,
        Guid newParentId,
        CancellationToken cancellationToken)
    {
        var currentId = newParentId;
        var visited = new HashSet<Guid> { itemId };

        while (true)
        {
            if (!visited.Add(currentId))
            {
                throw new BadRequestException(
                    MasterDataItemErrors.CircularParent,
                    "Circular parent-child relationship detected.");
            }

            var parent = await _unitOfWork.Repository<MasterDataItem, Guid>()
                .QueryReadOnly()
                .Where(i => i.Id == currentId && !i.IsDeleted)
                .Select(i => new { i.ParentItemId })
                .FirstOrDefaultAsync(cancellationToken);

            if (parent?.ParentItemId is null)
            {
                return;
            }

            currentId = parent.ParentItemId.Value;
        }
    }

    private async Task UnsetExistingDefaultAsync(Guid groupId, Guid exceptItemId, CancellationToken cancellationToken)
    {
        var existingDefaults = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .Query()
            .Where(i => i.GroupId == groupId && !i.IsDeleted && i.IsDefault && i.Id != exceptItemId)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingDefaults)
        {
            existing.SetDefault(false);
        }
    }

    private async Task EnqueueItemActivityAsync(
        string activityType,
        MasterDataItem item,
        string groupCode,
        CancellationToken cancellationToken)
    {
        await _activityLogService.EnqueuePostCommitAsync(
            activityType,
            $"Master data item {groupCode}/{item.Code}: {activityType} (ItemId={item.Id}, GroupId={item.GroupId})",
            AuditLogConstants.Modules.MasterData,
            cancellationToken: cancellationToken);
    }

    private static MasterDataItemDetailResponse MapDetail(MasterDataItem item, string groupCode) =>
        new()
        {
            Id = item.Id,
            GroupId = item.GroupId,
            GroupCode = groupCode,
            Code = item.Code,
            Name = item.Name,
            Value = item.Value,
            Description = item.Description,
            ParentItemId = item.ParentItemId,
            IsSystem = item.IsSystem,
            IsDefault = item.IsDefault,
            IsActive = item.IsActive,
            SortOrder = item.SortOrder,
            EffectiveFrom = item.EffectiveFrom,
            EffectiveTo = item.EffectiveTo,
            Metadata = item.Metadata,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };

    private void InvalidateLookupCache() =>
        _cacheInvalidationBuffer.EnqueueRemoveByPrefix(CacheKeys.MasterDataLookupPrefix);

    private static BadRequestException MapDomainException(DomainException ex) =>
        new(ex.Message, ex.ErrorCode ?? ex.Message);
}
