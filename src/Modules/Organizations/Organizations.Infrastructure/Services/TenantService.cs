using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Organizations.Application.Abstractions;
using Organizations.Application.Tenants;
using Organizations.Domain.OrganizationUsers;
using Organizations.Domain.Organizations;
using Organizations.Domain.Tenants;
using Organizations.Domain.WorkspaceUsers;
using Organizations.Domain.Workspaces;
using Organizations.Infrastructure.Persistence;
using OrganizationEntity = Organizations.Domain.Organizations.Organization;

namespace Organizations.Infrastructure.Services;

public sealed class TenantService : ITenantService
{
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<TenantService> _logger;

    public TenantService(
        OrganizationsUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<TenantService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        string code,
        string name,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = Tenant.NormalizeCode(code);
        if (await CodeExistsAsync(normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                TenantErrors.CodeAlreadyExists,
                $"Tenant code '{normalizedCode}' already exists.");
        }

        var tenant = Tenant.Create(
            normalizedCode,
            name,
            description,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<Tenant, Guid>().AddAsync(tenant, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created tenant: {tenant.Code} - {tenant.Name}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created tenant {TenantId} {Code}", tenant.Id, tenant.Code);
        return tenant.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantForUpdateAsync(id, cancellationToken);
        tenant.Update(name, description, metadata);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated tenant: {tenant.Code} - {tenant.Name}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantForUpdateAsync(id, cancellationToken);
        await EnsureTenantHasNoActiveDependenciesAsync(tenant.Id, cancellationToken);
        tenant.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted tenant: {tenant.Code} - {tenant.Name}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantForUpdateAsync(id, cancellationToken);
        try
        {
            tenant.Activate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(TenantErrors.AlreadyActive, $"Tenant '{tenant.Code}' is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated tenant: {tenant.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await GetTenantForUpdateAsync(id, cancellationToken);
        await EnsureTenantHasNoActiveDependenciesAsync(tenant.Id, cancellationToken);

        try
        {
            tenant.Deactivate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(TenantErrors.AlreadyInactive, $"Tenant '{tenant.Code}' is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated tenant: {tenant.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task<TenantDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Repository<Tenant, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        return tenant is null ? null : MapDetail(tenant);
    }

    public async Task<PagedResult<TenantListItemResponse>> GetListAsync(
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<Tenant, Guid>().QueryReadOnly().Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Code, pattern) ||
                EF.Functions.ILike(t.Name, pattern) ||
                EF.Functions.ILike(t.Description ?? string.Empty, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var projected = query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TenantListItemResponse
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Description = t.Description,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    private async Task<Tenant> GetTenantForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Repository<Tenant, Guid>()
            .Query()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(TenantErrors.NotFound, $"Tenant with id '{id}' was not found.");
        }

        return tenant;
    }

    private async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Tenant, Guid>()
            .QueryReadOnly()
            .Where(t => !t.IsDeleted && t.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    internal async Task EnsureTenantActiveAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.Repository<Tenant, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == tenantId && !t.IsDeleted, cancellationToken);

        if (tenant is null)
        {
            throw new NotFoundException(TenantErrors.NotFound, $"Tenant with id '{tenantId}' was not found.");
        }

        if (!tenant.IsActive)
        {
            throw new BadRequestException(TenantErrors.AlreadyInactive, "Tenant is inactive.");
        }
    }

    private async Task EnsureTenantHasNoActiveDependenciesAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var hasActiveOrganizations = await _unitOfWork.Repository<OrganizationEntity, Guid>()
            .QueryReadOnly()
            .AnyAsync(o => o.TenantId == tenantId && !o.IsDeleted && o.IsActive, cancellationToken);

        if (hasActiveOrganizations)
        {
            throw new ConflictException(
                TenantErrors.HasActiveDependencies,
                "Tenant has active organizations.");
        }

        var hasActiveWorkspaces = await _unitOfWork.Repository<Workspace, Guid>()
            .QueryReadOnly()
            .AnyAsync(w => w.TenantId == tenantId && !w.IsDeleted && w.IsActive, cancellationToken);

        if (hasActiveWorkspaces)
        {
            throw new ConflictException(
                TenantErrors.HasActiveDependencies,
                "Tenant has active workspaces.");
        }

        var hasActiveMemberships = await _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(m => m.TenantId == tenantId && !m.IsDeleted && m.IsActive, cancellationToken);

        if (hasActiveMemberships)
        {
            throw new ConflictException(
                TenantErrors.HasActiveDependencies,
                "Tenant has active organization memberships.");
        }

        var hasActiveWorkspaceUsers = await _unitOfWork.Repository<WorkspaceUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(u => u.TenantId == tenantId && !u.IsDeleted && u.IsActive, cancellationToken);

        if (hasActiveWorkspaceUsers)
        {
            throw new ConflictException(
                TenantErrors.HasActiveDependencies,
                "Tenant has active workspace users.");
        }
    }

    private static TenantDetailResponse MapDetail(Tenant tenant) =>
        new()
        {
            Id = tenant.Id,
            Code = tenant.Code,
            Name = tenant.Name,
            Description = tenant.Description,
            IsActive = tenant.IsActive,
            Metadata = tenant.Metadata,
            CreatedAt = tenant.CreatedAt,
            CreatedBy = tenant.CreatedBy,
            UpdatedAt = tenant.UpdatedAt,
            UpdatedBy = tenant.UpdatedBy
        };
}
