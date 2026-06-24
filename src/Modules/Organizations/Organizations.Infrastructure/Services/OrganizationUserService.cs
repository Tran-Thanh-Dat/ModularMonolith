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
using Organizations.Application.OrganizationUsers;
using Organizations.Domain.OrganizationUsers;
using Organizations.Infrastructure.Persistence;

namespace Organizations.Infrastructure.Services;

public sealed class OrganizationUserService : IOrganizationUserService
{
    private readonly OrganizationsUnitOfWork _unitOfWork;
    private readonly TenantService _tenantService;
    private readonly OrganizationService _organizationService;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<OrganizationUserService> _logger;

    public OrganizationUserService(
        OrganizationsUnitOfWork unitOfWork,
        TenantService tenantService,
        OrganizationService organizationService,
        IIdentityUserRepository identityUserRepository,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<OrganizationUserService> logger)
    {
        _unitOfWork = unitOfWork;
        _tenantService = tenantService;
        _organizationService = organizationService;
        _identityUserRepository = identityUserRepository;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> AssignAsync(
        Guid tenantId,
        Guid organizationId,
        Guid userId,
        bool isDefault,
        CancellationToken cancellationToken = default)
    {
        await _tenantService.EnsureTenantActiveAsync(tenantId, cancellationToken);
        var organization = await _organizationService.GetOrganizationEntityAsync(organizationId, cancellationToken);

        if (organization.TenantId != tenantId)
        {
            throw new BadRequestException(
                OrganizationErrors.ParentDifferentTenant,
                "Organization does not belong to the specified tenant.");
        }

        await _organizationService.EnsureOrganizationActiveAsync(organizationId, cancellationToken);
        await EnsureUserExistsAsync(userId, cancellationToken);

        if (await MembershipExistsAsync(organizationId, userId, null, cancellationToken))
        {
            throw new ConflictException(
                OrganizationUserErrors.AlreadyExists,
                "User is already assigned to this organization.");
        }

        var now = _dateTimeProvider.UtcNow;
        if (isDefault)
        {
            var existingMemberships = await _unitOfWork.Repository<OrganizationUser, Guid>()
                .Query()
                .Where(m => m.TenantId == tenantId && m.UserId == userId && !m.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingMemberships)
            {
                existing.SetDefault(false);
            }
        }

        var membership = OrganizationUser.Create(
            tenantId,
            organizationId,
            userId,
            isDefault,
            now,
            now,
            _currentUserService.UserId);

        await _unitOfWork.Repository<OrganizationUser, Guid>().AddAsync(membership, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Assigned user {userId} to organization {organizationId} (TenantId={tenantId})",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);

        return membership.Id;
    }

    public async Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);
        if (!membership.IsActive)
        {
            throw new BadRequestException(
                OrganizationUserErrors.AlreadyInactive,
                "Cannot set default on inactive membership.");
        }

        await ClearDefaultMembershipsAsync(membership.TenantId, membership.UserId, cancellationToken);
        membership.SetDefault(true);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Set default organization membership {id} for user {membership.UserId}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);
        await _tenantService.EnsureTenantActiveAsync(membership.TenantId, cancellationToken);
        await _organizationService.EnsureOrganizationActiveAsync(membership.OrganizationId, cancellationToken);

        try
        {
            membership.Activate(_dateTimeProvider.UtcNow);
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(OrganizationUserErrors.AlreadyActive, "Membership is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated organization membership {id}",
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
            throw new ConflictException(OrganizationUserErrors.AlreadyInactive, "Membership is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated organization membership {id}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await GetMembershipForUpdateAsync(id, cancellationToken);
        membership.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Removed organization membership {id}",
            AuditLogConstants.Modules.Organizations,
            cancellationToken: cancellationToken);
    }

    public async Task<OrganizationUserDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        return membership is null ? null : MapDetail(membership);
    }

    public async Task<PagedResult<OrganizationUserListItemResponse>> GetListAsync(
        Guid? tenantId,
        Guid? organizationId,
        Guid? userId,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<OrganizationUser, Guid>().QueryReadOnly().Where(m => !m.IsDeleted);

        if (tenantId.HasValue)
        {
            query = query.Where(m => m.TenantId == tenantId.Value);
        }

        if (organizationId.HasValue)
        {
            query = query.Where(m => m.OrganizationId == organizationId.Value);
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
            .Select(m => new OrganizationUserListItemResponse
            {
                Id = m.Id,
                TenantId = m.TenantId,
                OrganizationId = m.OrganizationId,
                UserId = m.UserId,
                IsDefault = m.IsDefault,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserListItemResponse>> GetUserOrganizationsAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .Where(m => !m.IsDeleted && m.UserId == userId);

        if (tenantId.HasValue)
        {
            query = query.Where(m => m.TenantId == tenantId.Value);
        }

        return await query
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.JoinedAt)
            .Select(m => new OrganizationUserListItemResponse
            {
                Id = m.Id,
                TenantId = m.TenantId,
                OrganizationId = m.OrganizationId,
                UserId = m.UserId,
                IsDefault = m.IsDefault,
                IsActive = m.IsActive,
                JoinedAt = m.JoinedAt,
                LeftAt = m.LeftAt
            })
            .ToListAsync(cancellationToken);
    }

    internal async Task<bool> HasActiveOrganizationMembershipAsync(
        Guid tenantId,
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .AnyAsync(
                m => m.TenantId == tenantId &&
                     m.OrganizationId == organizationId &&
                     m.UserId == userId &&
                     !m.IsDeleted &&
                     m.IsActive,
                cancellationToken);
    }

    private async Task EnsureUserExistsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _identityUserRepository.FindActiveByIdForUpdateAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(OrganizationUserErrors.UserNotFound, $"User with id '{userId}' was not found.");
        }
    }

    private async Task ClearDefaultMembershipsAsync(
        Guid tenantId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var defaults = await _unitOfWork.Repository<OrganizationUser, Guid>()
            .Query()
            .Where(m =>
                m.TenantId == tenantId &&
                m.UserId == userId &&
                !m.IsDeleted &&
                m.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var membership in defaults)
        {
            membership.SetDefault(false);
        }
    }

    private async Task<bool> MembershipExistsAsync(
        Guid organizationId,
        Guid userId,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<OrganizationUser, Guid>()
            .QueryReadOnly()
            .Where(m => !m.IsDeleted && m.OrganizationId == organizationId && m.UserId == userId);

        if (excludeId.HasValue)
        {
            query = query.Where(m => m.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private async Task<OrganizationUser> GetMembershipForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var membership = await _unitOfWork.Repository<OrganizationUser, Guid>()
            .Query()
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        if (membership is null)
        {
            throw new NotFoundException(OrganizationUserErrors.NotFound, $"Organization membership '{id}' was not found.");
        }

        return membership;
    }

    private static OrganizationUserListItemResponse MapListItem(OrganizationUser membership) =>
        new()
        {
            Id = membership.Id,
            TenantId = membership.TenantId,
            OrganizationId = membership.OrganizationId,
            UserId = membership.UserId,
            IsDefault = membership.IsDefault,
            IsActive = membership.IsActive,
            JoinedAt = membership.JoinedAt,
            LeftAt = membership.LeftAt
        };

    private static OrganizationUserDetailResponse MapDetail(OrganizationUser membership) =>
        new()
        {
            Id = membership.Id,
            TenantId = membership.TenantId,
            OrganizationId = membership.OrganizationId,
            UserId = membership.UserId,
            IsDefault = membership.IsDefault,
            IsActive = membership.IsActive,
            JoinedAt = membership.JoinedAt,
            LeftAt = membership.LeftAt,
            CreatedAt = membership.CreatedAt,
            CreatedBy = membership.CreatedBy
        };
}
