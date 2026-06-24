using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;
using Organizations.Domain.Enums;
using Organizations.Domain.OrganizationUsers;
using Organizations.Domain.Tenants;
using Organizations.Domain.Workspaces;
using Organizations.Infrastructure.Persistence;
using OrganizationEntity = Organizations.Domain.Organizations.Organization;

namespace Organizations.Infrastructure.Services;

public sealed class OrganizationService : IOrganizationService
{
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly TenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        OrganizationsUnitOfWork unitOfWork,
        TenantService tenantService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<OrganizationService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        Guid? parentOrganizationId,
        string code,
        string name,
        string? description,
        OrganizationType type,
        int sortOrder,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        await _tenantService.EnsureTenantActiveAsync(tenantId, cancellationToken);

        OrganizationEntity? parent = null;
        if (parentOrganizationId.HasValue)
        {
            parent = await GetOrganizationEntityAsync(parentOrganizationId.Value, cancellationToken);
            ValidateParent(tenantId, parent, null);
        }

        var normalizedCode = OrganizationEntity.NormalizeCode(code);
        if (await CodeExistsAsync(tenantId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                OrganizationErrors.CodeAlreadyExists,
                $"Organization code '{normalizedCode}' already exists for tenant.");
        }

        var organization = OrganizationEntity.Create(
            tenantId,
            parentOrganizationId,
            normalizedCode,
            name,
            description,
            type,
            sortOrder,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<OrganizationEntity, Guid>().AddAsync(organization, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created organization: {organization.Code} (TenantId={organization.TenantId})",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);

        return organization.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        OrganizationType type,
        int sortOrder,
        string? metadata,
        Guid? parentOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var organization = await GetOrganizationForUpdateAsync(id, cancellationToken);

        if (parentOrganizationId == id)
        {
            throw new BadRequestException(
                OrganizationErrors.ParentSelfReference,
                "Organization cannot be its own parent.");
        }

        if (parentOrganizationId.HasValue)
        {
            var parent = await GetOrganizationEntityAsync(parentOrganizationId.Value, cancellationToken);
            ValidateParent(organization.TenantId, parent, id);
            await EnsureNoCircularParentAsync(id, parentOrganizationId.Value, cancellationToken);
        }

        organization.Update(name, description, type, sortOrder, metadata, parentOrganizationId);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated organization: {organization.Code} (TenantId={organization.TenantId})",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await GetOrganizationForUpdateAsync(id, cancellationToken);
        await EnsureOrganizationHasNoActiveDependenciesAsync(organization.Id, cancellationToken);
        organization.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted organization: {organization.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await GetOrganizationForUpdateAsync(id, cancellationToken);
        await _tenantService.EnsureTenantActiveAsync(organization.TenantId, cancellationToken);

        if (organization.ParentOrganizationId.HasValue)
        {
            var parent = await GetOrganizationEntityAsync(organization.ParentOrganizationId.Value, cancellationToken);
            if (!parent.IsActive)
            {
                throw new BadRequestException(
                    OrganizationErrors.ParentInactive,
                    "Cannot activate organization while parent is inactive.");
            }
        }

        try
        {
            organization.Activate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(OrganizationErrors.AlreadyActive, "Organization is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated organization: {organization.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await GetOrganizationForUpdateAsync(id, cancellationToken);
        await EnsureOrganizationHasNoActiveDependenciesAsync(organization.Id, cancellationToken);

        try
        {
            organization.Deactivate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(OrganizationErrors.AlreadyInactive, "Organization is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated organization: {organization.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task<OrganizationDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var organization = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        return organization is null ? null : MapDetail(organization);
    }

    public async Task<PagedResult<OrganizationListItemResponse>> GetListAsync(
        Guid? tenantId,
        Guid? parentOrganizationId,
        string? keyword,
        bool? isActive,
        OrganizationType? type,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<OrganizationEntity, Guid>().QueryReadOnly().Where(o => !o.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(o => o.TenantId == tenantId.Value);
        }

        if (parentOrganizationId.HasValue)
        {
            query = query.Where(o => o.ParentOrganizationId == parentOrganizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.Code, pattern) ||
                EF.Functions.ILike(o.Name, pattern) ||
                EF.Functions.ILike(o.Description ?? string.Empty, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(o => o.IsActive == isActive.Value);
        }

        if (type.HasValue)
        {
            query = query.Where(o => o.Type == type.Value);
        }

        var projected = query
            .OrderBy(o => o.SortOrder)
            .ThenByDescending(o => o.CreatedAt)
            .Select(o => new OrganizationListItemResponse
            {
                Id = o.Id,
                TenantId = o.TenantId,
                ParentOrganizationId = o.ParentOrganizationId,
                Code = o.Code,
                Name = o.Name,
                Type = o.Type,
                IsActive = o.IsActive,
                SortOrder = o.SortOrder,
                CreatedAt = o.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationTreeNodeResponse>> GetTreeAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var flat = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .QueryReadOnly()
            .Where(o => o.TenantId == tenantId && !o.IsDeleted)
            .OrderBy(o => o.SortOrder)
            .ThenBy(o => o.Name)
            .Select(o => new TreeSource(
                o.Id,
                o.ParentOrganizationId,
                o.Code,
                o.Name,
                o.Type,
                o.IsActive,
                o.SortOrder))
            .ToListAsync(cancellationToken);

        var childrenByParent = flat
            .Where(o => o.ParentOrganizationId.HasValue)
            .GroupBy(o => o.ParentOrganizationId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        OrganizationTreeNodeResponse BuildNode(TreeSource item)
        {
            var children = childrenByParent.GetValueOrDefault(item.Id)?
                .Select(BuildNode)
                .ToList() ?? [];

            return new OrganizationTreeNodeResponse
            {
                Id = item.Id,
                ParentOrganizationId = item.ParentOrganizationId,
                Code = item.Code,
                Name = item.Name,
                Type = item.Type,
                IsActive = item.IsActive,
                SortOrder = item.SortOrder,
                Children = children
            };
        }

        return flat
            .Where(o => !o.ParentOrganizationId.HasValue)
            .Select(BuildNode)
            .ToList();
    }

    private sealed record TreeSource(
        Guid Id,
        Guid? ParentOrganizationId,
        string Code,
        string Name,
        OrganizationType Type,
        bool IsActive,
        int SortOrder);

    internal async Task<OrganizationEntity> GetOrganizationEntityAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var organization = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .Query()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization is null)
        {
            throw new NotFoundException(OrganizationErrors.NotFound, $"Organization with id '{id}' was not found.");
        }

        return organization;
    }

    internal async Task EnsureOrganizationActiveAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        var organization = await GetOrganizationEntityAsync(organizationId, cancellationToken);
        if (!organization.IsActive)
        {
            throw new BadRequestException(OrganizationErrors.Inactive, "Organization is inactive.");
        }
    }

    private async Task<OrganizationEntity> GetOrganizationForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var organization = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .Query()
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted, cancellationToken);

        if (organization is null)
        {
            throw new NotFoundException(OrganizationErrors.NotFound, $"Organization with id '{id}' was not found.");
        }

        return organization;
    }

    private static void ValidateParent(Guid tenantId, OrganizationEntity parent, Guid? organizationId)
    {
        if (parent.TenantId != tenantId)
        {
            throw new BadRequestException(
                OrganizationErrors.ParentDifferentTenant,
                "Parent organization belongs to a different tenant.");
        }

        if (organizationId.HasValue && parent.Id == organizationId.Value)
        {
            throw new BadRequestException(
                OrganizationErrors.ParentSelfReference,
                "Organization cannot be its own parent.");
        }

        if (!parent.IsActive)
        {
            throw new BadRequestException(
                OrganizationErrors.ParentInactive,
                "Parent organization is inactive.");
        }
    }

    private async Task EnsureNoCircularParentAsync(
        Guid organizationId,
        Guid newParentId,
        CancellationToken cancellationToken)
    {
        var currentId = newParentId;
        var visited = new HashSet<Guid> { organizationId };

        while (true)
        {
            if (!visited.Add(currentId))
            {
                throw new BadRequestException(
                    OrganizationErrors.CircularParent,
                    "Circular parent-child relationship detected.");
            }

            var parent = await _unitOfWork.Repository<OrganizationEntity, Guid>()
                .QueryReadOnly()
                .Where(o => o.Id == currentId && !o.IsDeleted)
                .Select(o => o.ParentOrganizationId)
                .FirstOrDefaultAsync(cancellationToken);

            if (parent is null)
            {
                return;
            }

            currentId = parent.Value;
        }
    }

    private async Task<bool> CodeExistsAsync(
        Guid tenantId,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<OrganizationEntity, Guid>()
            .QueryReadOnly()
            .Where(o => !o.IsDeleted && o.TenantId == tenantId && o.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(o => o.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task EnsureOrganizationHasNoActiveDependenciesAsync(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        var hasActiveChildren = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .QueryReadOnly()
            .AnyAsync(o => o.ParentOrganizationId == organizationId && !o.IsDeleted && o.IsActive, cancellationToken);

        if (hasActiveChildren)
        {
            throw new ConflictException(
                OrganizationErrors.HasActiveDependencies,
                "Organization has active child organizations.");
        }

        var hasActiveWorkspaces = await _unitOfWork.Repository<Workspace, Guid>()
            .QueryReadOnly()
            .AnyAsync(w => w.OrganizationId == organizationId && !w.IsDeleted && w.IsActive, cancellationToken);

        if (hasActiveWorkspaces)
        {
            throw new ConflictException(
                OrganizationErrors.HasActiveDependencies,
                "Organization has active workspaces.");
        }

        var hasActiveMemberships = await _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(m => m.OrganizationId == organizationId && !m.IsDeleted && m.IsActive, cancellationToken);

        if (hasActiveMemberships)
        {
            throw new ConflictException(
                OrganizationErrors.HasActiveDependencies,
                "Organization has active memberships.");
        }
    }

    private static OrganizationDetailResponse MapDetail(OrganizationEntity organization) =>
        new()
        {
            Id = organization.Id,
            TenantId = organization.TenantId,
            ParentOrganizationId = organization.ParentOrganizationId,
            Code = organization.Code,
            Name = organization.Name,
            Description = organization.Description,
            Type = organization.Type,
            IsActive = organization.IsActive,
            SortOrder = organization.SortOrder,
            Metadata = organization.Metadata,
            CreatedAt = organization.CreatedAt,
            CreatedBy = organization.CreatedBy,
            UpdatedAt = organization.UpdatedAt,
            UpdatedBy = organization.UpdatedBy
        };
}
