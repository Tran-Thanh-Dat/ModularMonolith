using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Organizations.Application.Abstractions;
using Organizations.Application.Workspaces;
using Organizations.Domain.WorkspaceUsers;
using Organizations.Domain.Workspaces;
using Organizations.Infrastructure.Persistence;

namespace Organizations.Infrastructure.Services;

public sealed class WorkspaceService : IWorkspaceService
{
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly TenantService _tenantService;
    private readonly OrganizationService _organizationService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(
        OrganizationsUnitOfWork unitOfWork,
        TenantService tenantService,
        OrganizationService organizationService,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<WorkspaceService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _organizationService = organizationService;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        Guid tenantId,
        Guid? organizationId,
        string code,
        string name,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        await _tenantService.EnsureTenantActiveAsync(tenantId, cancellationToken);

        if (organizationId.HasValue)
        {
            var organization = await _organizationService.GetOrganizationEntityAsync(organizationId.Value, cancellationToken);
            if (organization.TenantId != tenantId)
            {
                throw new BadRequestException(
                    WorkspaceErrors.OrganizationDifferentTenant,
                    "Organization belongs to a different tenant.");
            }

            await _organizationService.EnsureOrganizationActiveAsync(organizationId.Value, cancellationToken);
        }

        var normalizedCode = Workspace.NormalizeCode(code);
        if (await CodeExistsAsync(tenantId, normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                WorkspaceErrors.CodeAlreadyExists,
                $"Workspace code '{normalizedCode}' already exists for tenant.");
        }

        var workspace = Workspace.Create(
            tenantId,
            organizationId,
            normalizedCode,
            name,
            description,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<Workspace, Guid>().AddAsync(workspace, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created workspace: {workspace.Code} (TenantId={tenantId})",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);

        return workspace.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        Guid? organizationId,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceForUpdateAsync(id, cancellationToken);

        if (organizationId.HasValue)
        {
            var organization = await _organizationService.GetOrganizationEntityAsync(organizationId.Value, cancellationToken);
            if (organization.TenantId != workspace.TenantId)
            {
                throw new BadRequestException(
                    WorkspaceErrors.OrganizationDifferentTenant,
                    "Organization belongs to a different tenant.");
            }

            await _organizationService.EnsureOrganizationActiveAsync(organizationId.Value, cancellationToken);
        }

        workspace.Update(name, description, organizationId, metadata);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated workspace: {workspace.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceForUpdateAsync(id, cancellationToken);

        var hasActiveUsers = await _unitOfWork.Repository<WorkspaceUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(u => u.WorkspaceId == id && !u.IsDeleted && u.IsActive, cancellationToken);

        if (hasActiveUsers)
        {
            throw new ConflictException(
                WorkspaceErrors.HasActiveDependencies,
                "Workspace has active members.");
        }

        workspace.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted workspace: {workspace.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceForUpdateAsync(id, cancellationToken);
        await _tenantService.EnsureTenantActiveAsync(workspace.TenantId, cancellationToken);

        if (workspace.OrganizationId.HasValue)
        {
            await _organizationService.EnsureOrganizationActiveAsync(workspace.OrganizationId.Value, cancellationToken);
        }

        try
        {
            workspace.Activate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(WorkspaceErrors.AlreadyActive, "Workspace is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated workspace: {workspace.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await GetWorkspaceForUpdateAsync(id, cancellationToken);

        var hasActiveUsers = await _unitOfWork.Repository<WorkspaceUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(u => u.WorkspaceId == id && !u.IsDeleted && u.IsActive, cancellationToken);

        if (hasActiveUsers)
        {
            throw new ConflictException(
                WorkspaceErrors.HasActiveDependencies,
                "Workspace has active members.");
        }

        try
        {
            workspace.Deactivate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(WorkspaceErrors.AlreadyInactive, "Workspace is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated workspace: {workspace.Code}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task<WorkspaceDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workspace = await _unitOfWork.Repository<Workspace, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken);

        return workspace is null ? null : MapDetail(workspace);
    }

    public async Task<PagedResult<WorkspaceListItemResponse>> GetListAsync(
        Guid? tenantId,
        Guid? organizationId,
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<Workspace, Guid>().QueryReadOnly().Where(w => !w.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(w => w.TenantId == tenantId.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(w => w.OrganizationId == organizationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(w =>
                EF.Functions.ILike(w.Code, pattern) ||
                EF.Functions.ILike(w.Name, pattern) ||
                EF.Functions.ILike(w.Description ?? string.Empty, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(w => w.IsActive == isActive.Value);
        }

        var projected = query
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WorkspaceListItemResponse
            {
                Id = w.Id,
                TenantId = w.TenantId,
                OrganizationId = w.OrganizationId,
                Code = w.Code,
                Name = w.Name,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    internal async Task<Workspace> GetWorkspaceEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Repository<Workspace, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken);

        if (workspace is null)
        {
            throw new NotFoundException(WorkspaceErrors.NotFound, $"Workspace with id '{id}' was not found.");
        }

        return workspace;
    }

    internal async Task EnsureWorkspaceActiveAsync(Guid workspaceId, CancellationToken cancellationToken)
    {
        var workspace = await GetWorkspaceEntityAsync(workspaceId, cancellationToken);
        if (!workspace.IsActive)
        {
            throw new BadRequestException(WorkspaceErrors.Inactive, "Workspace is inactive.");
        }
    }

    private async Task<Workspace> GetWorkspaceForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var workspace = await _unitOfWork.Repository<Workspace, Guid>()
            .Query()
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted, cancellationToken);

        if (workspace is null)
        {
            throw new NotFoundException(WorkspaceErrors.NotFound, $"Workspace with id '{id}' was not found.");
        }

        return workspace;
    }

    private async Task<bool> CodeExistsAsync(
        Guid tenantId,
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Workspace, Guid>()
            .QueryReadOnly()
            .Where(w => !w.IsDeleted && w.TenantId == tenantId && w.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(w => w.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static WorkspaceDetailResponse MapDetail(Workspace workspace) =>
        new()
        {
            Id = workspace.Id,
            TenantId = workspace.TenantId,
            OrganizationId = workspace.OrganizationId,
            Code = workspace.Code,
            Name = workspace.Name,
            Description = workspace.Description,
            IsActive = workspace.IsActive,
            Metadata = workspace.Metadata,
            CreatedAt = workspace.CreatedAt,
            CreatedBy = workspace.CreatedBy,
            UpdatedAt = workspace.UpdatedAt,
            UpdatedBy = workspace.UpdatedBy
        };
}
