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
using MasterData.Domain.Enums;
using MasterData.Domain.Errors;
using MasterData.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MasterData.Infrastructure.Services;

public sealed class MasterDataGroupService : IMasterDataGroupService
{
    private readonly MasterDataUnitOfWork _unitOfWork;
    private readonly MasterDataScopeValidator _scopeValidator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<MasterDataGroupService> _logger;

    public MasterDataGroupService(
        MasterDataUnitOfWork unitOfWork,
        MasterDataScopeValidator scopeValidator,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<MasterDataGroupService> logger)
    {
        _unitOfWork = unitOfWork;
        _scopeValidator = scopeValidator;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        string code,
        string name,
        string? description,
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        int sortOrder,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        await _scopeValidator.ValidateScopeAsync(scope, tenantId, organizationId, cancellationToken);

        var normalizedCode = MasterDataGroup.NormalizeCode(code);
        if (await GroupCodeExistsAsync(scope, tenantId, organizationId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                MasterDataGroupErrors.CodeAlreadyExists,
                $"Master data group code '{normalizedCode}' already exists for this scope.");
        }

        var group = MasterDataGroup.Create(
            normalizedCode,
            name,
            description,
            scope,
            tenantId,
            organizationId,
            isSystem: false,
            sortOrder,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<MasterDataGroup, Guid>().AddAsync(group, cancellationToken);

        await EnqueueGroupActivityAsync(
            MasterDataActivityTypes.GroupCreated,
            group,
            cancellationToken);

        InvalidateLookupCache();
        _logger.LogInformation("Created master data group {GroupId} {Code}", group.Id, group.Code);
        return group.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string? code,
        string name,
        string? description,
        int sortOrder,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var group = await GetGroupForUpdateAsync(id, cancellationToken);

        if (!string.IsNullOrWhiteSpace(code) &&
            !string.Equals(group.Code, MasterDataGroup.NormalizeCode(code), StringComparison.Ordinal))
        {
            try
            {
                group.ChangeCode(code);
            }
            catch (DomainException ex)
            {
                throw MapDomainException(ex);
            }

            if (await GroupCodeExistsAsync(group.Scope, group.TenantId, group.OrganizationId, group.Code, group.Id, cancellationToken))
            {
                throw new ConflictException(
                    MasterDataGroupErrors.CodeAlreadyExists,
                    $"Master data group code '{group.Code}' already exists for this scope.");
            }
        }

        group.Update(name, description, sortOrder, metadata);

        await EnqueueGroupActivityAsync(
            MasterDataActivityTypes.GroupUpdated,
            group,
            cancellationToken);

        InvalidateLookupCache();
        _logger.LogInformation("Updated master data group {GroupId}", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await GetGroupForUpdateAsync(id, cancellationToken);

        if (group.IsSystem)
        {
            throw new BadRequestException(
                MasterDataGroupErrors.SystemProtected,
                "System group cannot be deleted.");
        }

        var hasActiveItems = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .AnyAsync(i => i.GroupId == id && !i.IsDeleted && i.IsActive, cancellationToken);

        if (hasActiveItems)
        {
            throw new ConflictException(
                MasterDataGroupErrors.HasActiveItems,
                "Cannot delete group while active items exist.");
        }

        try
        {
            group.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);
        }
        catch (DomainException ex)
        {
            throw MapDomainException(ex);
        }

        await EnqueueGroupActivityAsync(
            MasterDataActivityTypes.GroupDeleted,
            group,
            cancellationToken);

        InvalidateLookupCache();
        _logger.LogInformation("Deleted master data group {GroupId}", id);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await GetGroupForUpdateAsync(id, cancellationToken);

        try
        {
            group.Activate();
        }
        catch (DomainException)
        {
            throw new ConflictException(
                MasterDataGroupErrors.AlreadyActive,
                $"Master data group '{group.Code}' is already active.");
        }

        await EnqueueGroupActivityAsync(
            MasterDataActivityTypes.GroupActivated,
            group,
            cancellationToken);

        InvalidateLookupCache();
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await GetGroupForUpdateAsync(id, cancellationToken);

        var hasActiveItems = await _unitOfWork.Repository<MasterDataItem, Guid>()
            .QueryReadOnly()
            .AnyAsync(i => i.GroupId == id && !i.IsDeleted && i.IsActive, cancellationToken);

        if (hasActiveItems)
        {
            throw new ConflictException(
                MasterDataGroupErrors.HasActiveItems,
                "Cannot deactivate group while active items exist.");
        }

        try
        {
            group.Deactivate();
        }
        catch (DomainException)
        {
            throw new ConflictException(
                MasterDataGroupErrors.AlreadyInactive,
                $"Master data group '{group.Code}' is already inactive.");
        }

        await EnqueueGroupActivityAsync(
            MasterDataActivityTypes.GroupDeactivated,
            group,
            cancellationToken);

        InvalidateLookupCache();
    }

    public async Task<MasterDataGroupDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

        return group is null ? null : MapDetail(group);
    }

    public async Task<PagedResult<MasterDataGroupListItemResponse>> GetListAsync(
        MasterDataGroupListQuery query,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = query.PageIndex, PageSize = query.PageSize };
        var dbQuery = _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .Where(g => !g.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Keyword))
        {
            var pattern = $"%{query.Keyword.Trim()}%";
            dbQuery = dbQuery.Where(g =>
                EF.Functions.ILike(g.Code, pattern) ||
                EF.Functions.ILike(g.Name, pattern) ||
                EF.Functions.ILike(g.Description ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.Code))
        {
            var code = MasterDataGroup.NormalizeCode(query.Code);
            dbQuery = dbQuery.Where(g => g.Code == code);
        }

        if (query.Scope.HasValue)
        {
            dbQuery = dbQuery.Where(g => g.Scope == query.Scope.Value);
        }

        if (query.TenantId.HasValue)
        {
            dbQuery = dbQuery.Where(g => g.TenantId == query.TenantId.Value);
        }

        if (query.OrganizationId.HasValue)
        {
            dbQuery = dbQuery.Where(g => g.OrganizationId == query.OrganizationId.Value);
        }

        if (query.IsSystem.HasValue)
        {
            dbQuery = dbQuery.Where(g => g.IsSystem == query.IsSystem.Value);
        }

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(g => g.IsActive == query.IsActive.Value);
        }

        var projected = dbQuery
            .OrderBy(g => g.SortOrder)
            .ThenBy(g => g.Code)
            .Select(g => new MasterDataGroupListItemResponse
            {
                Id = g.Id,
                Code = g.Code,
                Name = g.Name,
                Scope = g.Scope,
                TenantId = g.TenantId,
                OrganizationId = g.OrganizationId,
                IsSystem = g.IsSystem,
                IsActive = g.IsActive,
                SortOrder = g.SortOrder
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    private async Task<MasterDataGroup> GetGroupForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var group = await _unitOfWork.Repository<MasterDataGroup, Guid>()
            .Query()
            .FirstOrDefaultAsync(g => g.Id == id && !g.IsDeleted, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException(
                MasterDataGroupErrors.NotFound,
                $"Master data group with id '{id}' was not found.");
        }

        return group;
    }

    private async Task<bool> GroupCodeExistsAsync(
        MasterDataScope scope,
        Guid? tenantId,
        Guid? organizationId,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<MasterDataGroup, Guid>()
            .QueryReadOnly()
            .Where(g => !g.IsDeleted && g.Scope == scope && g.Code == code);

        query = scope switch
        {
            MasterDataScope.Global => query.Where(g => g.TenantId == null && g.OrganizationId == null),
            MasterDataScope.Tenant => query.Where(g => g.TenantId == tenantId && g.OrganizationId == null),
            MasterDataScope.Organization => query.Where(g => g.TenantId == tenantId && g.OrganizationId == organizationId),
            _ => query
        };

        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task EnqueueGroupActivityAsync(
        string activityType,
        MasterDataGroup group,
        CancellationToken cancellationToken)
    {
        await _activityLogService.EnqueuePostCommitAsync(
            activityType,
            $"Master data group {group.Code}: {activityType} (GroupId={group.Id}, Scope={group.Scope})",
            AuditLogConstants.Modules.MasterData,
            cancellationToken: cancellationToken);
    }

    private static MasterDataGroupDetailResponse MapDetail(MasterDataGroup group) =>
        new()
        {
            Id = group.Id,
            Code = group.Code,
            Name = group.Name,
            Description = group.Description,
            Scope = group.Scope,
            TenantId = group.TenantId,
            OrganizationId = group.OrganizationId,
            IsSystem = group.IsSystem,
            IsActive = group.IsActive,
            SortOrder = group.SortOrder,
            Metadata = group.Metadata,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt
        };

    private void InvalidateLookupCache() =>
        _cacheInvalidationBuffer.EnqueueRemoveByPrefix(CacheKeys.MasterDataLookupPrefix);

    private static BadRequestException MapDomainException(DomainException ex) =>
        new(ex.Message, ex.ErrorCode ?? ex.Message);
}
