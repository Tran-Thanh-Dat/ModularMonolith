using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Organizations.Application.Abstractions;
using Organizations.Application.WorkspaceUsers;
using Organizations.Domain.WorkspaceUsers;
using Organizations.Infrastructure.Persistence;

namespace Organizations.Infrastructure.Services;

public sealed class WorkspaceUserService : IWorkspaceUserService
{
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly TenantService _tenantService;
    private readonly WorkspaceService _workspaceService;
    private readonly OrganizationUserService _organizationUserService;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<WorkspaceUserService> _logger;

    public WorkspaceUserService(
        OrganizationsUnitOfWork unitOfWork,
        TenantService tenantService,
        WorkspaceService workspaceService,
        OrganizationUserService organizationUserService,
        IIdentityUserRepository identityUserRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<WorkspaceUserService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _workspaceService = workspaceService;
        _organizationUserService = organizationUserService;
        _identityUserRepository = identityUserRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> AssignAsync(
        Guid tenantId,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _tenantService.EnsureTenantActiveAsync(tenantId, cancellationToken);
        var workspace = await _workspaceService.GetWorkspaceEntityAsync(workspaceId, cancellationToken);

        if (workspace.TenantId != tenantId)
        {
            throw new BadRequestException(
                WorkspaceErrors.OrganizationDifferentTenant,
                "Workspace does not belong to the specified tenant.");
        }

        await _workspaceService.EnsureWorkspaceActiveAsync(workspaceId, cancellationToken);
        await EnsureUserExistsAsync(userId, cancellationToken);

        if (workspace.OrganizationId.HasValue)
        {
            var hasMembership = await _organizationUserService.HasActiveOrganizationMembershipAsync(
                tenantId,
                workspace.OrganizationId.Value,
                userId,
                cancellationToken);

            if (!hasMembership)
            {
                throw new BadRequestException(
                    WorkspaceUserErrors.OrganizationMembershipRequired,
                    "User must be an active member of the workspace organization.");
            }
        }

        if (await MembershipExistsAsync(workspaceId, userId, null, cancellationToken))
        {
            throw new ConflictException(
                WorkspaceUserErrors.AlreadyExists,
                "User is already assigned to this workspace.");
        }

        var now = _dateTimeProvider.UtcNow;
        var membership = WorkspaceUser.Create(
            tenantId,
            workspaceId,
            userId,
            now,
            now,
            _currentUserService.UserId);

        await _unitOfWork.Repository<WorkspaceUser, Guid>().AddAsync(membership, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Assigned user {userId} to workspace {workspaceId} (TenantId={tenantId})",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);

        return membership.Id;
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);
        await _tenantService.EnsureTenantActiveAsync(membership.TenantId, cancellationToken);
        await _workspaceService.EnsureWorkspaceActiveAsync(membership.WorkspaceId, cancellationToken);

        try
        {
            membership.Activate(_dateTimeProvider.UtcNow);
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(WorkspaceUserErrors.AlreadyActive, "Membership is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated workspace membership {id}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);

        try
        {
            membership.Deactivate(_dateTimeProvider.UtcNow);
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(WorkspaceUserErrors.AlreadyInactive, "Membership is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated workspace membership {id}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);
        membership.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Removed workspace membership {id}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task<WorkspaceUserDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await _unitOfWork.Repository<WorkspaceUser, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        return membership is null ? null : MapDetail(membership);
    }

    public async Task<PagedResult<WorkspaceUserListItemResponse>> GetListAsync(
        Guid? tenantId,
        Guid? workspaceId,
        Guid? userId,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<WorkspaceUser, Guid>().QueryReadOnly().Where(m => !m.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(m => m.TenantId == tenantId.Value);
        }

        if (workspaceId.HasValue)
        {
            query = query.Where(m => m.WorkspaceId == workspaceId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(m => m.UserId == userId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        var projected = query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new WorkspaceUserListItemResponse
            {
                Id = m.Id,
                TenantId = m.TenantId,
                WorkspaceId = m.WorkspaceId,
                UserId = m.UserId,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _identityUserRepository.FindActiveByIdForUpdateAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(WorkspaceUserErrors.UserNotFound, $"User with id '{userId}' was not found.");
        }
    }

    private async Task<bool> MembershipExistsAsync(
        Guid workspaceId,
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<WorkspaceUser, Guid>()
            .QueryReadOnly()
            .Where(m => !m.IsDeleted && m.WorkspaceId == workspaceId && m.UserId == userId);

        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task<WorkspaceUser> GetMembershipForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var membership = await _unitOfWork.Repository<WorkspaceUser, Guid>()
            .Query()
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException(WorkspaceUserErrors.NotFound, $"Workspace membership '{id}' was not found.");
        }

        return membership;
    }

    private static WorkspaceUserDetailResponse MapDetail(WorkspaceUser membership) =>
        new()
        {
            Id = membership.Id,
            TenantId = membership.TenantId,
            WorkspaceId = membership.WorkspaceId,
            UserId = membership.UserId,
            IsActive = membership.IsActive,
            JoinedAt = membership.JoinedAt,
            LeftAt = membership.LeftAt,
            CreatedAt = membership.CreatedAt,
            CreatedBy = membership.CreatedBy
        };
}
