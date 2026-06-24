using AuditLogs.Application.Abstractions;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.RolePermissionPolicies;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class RolePermissionPolicyService : IRolePermissionPolicyService
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<RolePermissionPolicyService> _logger;

    public RolePermissionPolicyService(
        AuthorizationPoliciesUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<RolePermissionPolicyService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Guid> AssignAsync(Guid roleId, Guid permissionPolicyId, CancellationToken cancellationToken = default)
    {
        var policy = await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .FirstOrDefaultAsync(p => p.Id == permissionPolicyId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(PermissionPolicyErrors.NotFound, "Permission policy not found.");

        if (!policy.IsActive)
        {
            throw new BadRequestException(RolePermissionPolicyErrors.PolicyInactive, "Cannot assign inactive permission policy.");
        }

        var repo = _unitOfWork.Repository<RolePermissionPolicy, Guid>();
        if (await repo.Query().AnyAsync(r => !r.IsDeleted && r.RoleId == roleId && r.PermissionPolicyId == permissionPolicyId, cancellationToken))
        {
            throw new ConflictException(RolePermissionPolicyErrors.AlreadyExists, "Role permission policy assignment already exists.");
        }

        var assignment = RolePermissionPolicy.Create(roleId, permissionPolicyId, _dateTimeProvider.UtcNow, _currentUserService.UserId);
        await repo.AddAsync(assignment, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.RolePermissionPolicyAssigned,
            $"Assigned permission policy {policy.Code} to role {roleId}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Assigned policy {PolicyId} to role {RoleId}", permissionPolicyId, roleId);
        return assignment.Id;
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await GetEntityAsync(id, cancellationToken);
        assignment.Activate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.RolePermissionPolicyActivated,
            $"Activated role permission policy assignment {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await GetEntityAsync(id, cancellationToken);
        assignment.Deactivate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.RolePermissionPolicyDeactivated,
            $"Deactivated role permission policy assignment {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await GetEntityAsync(id, cancellationToken);
        assignment.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.RolePermissionPolicyRemoved,
            $"Removed role permission policy assignment {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<RolePermissionPolicyListItemResponse>> GetListAsync(
        Guid? roleId,
        Guid? permissionPolicyId,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = from r in _unitOfWork.Repository<RolePermissionPolicy, Guid>().Query().AsNoTracking()
                    join p in _unitOfWork.Repository<PermissionPolicy, Guid>().Query().AsNoTracking()
                        on r.PermissionPolicyId equals p.Id
                    where !r.IsDeleted && !p.IsDeleted
                    select new { Assignment = r, Policy = p };

        if (roleId.HasValue)
        {
            query = query.Where(x => x.Assignment.RoleId == roleId.Value);
        }

        if (permissionPolicyId.HasValue)
        {
            query = query.Where(x => x.Assignment.PermissionPolicyId == permissionPolicyId.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.Assignment.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Assignment.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RolePermissionPolicyListItemResponse
            {
                Id = x.Assignment.Id,
                RoleId = x.Assignment.RoleId,
                PermissionPolicyId = x.Assignment.PermissionPolicyId,
                PermissionPolicyCode = x.Policy.Code,
                PermissionCode = x.Policy.PermissionCode,
                IsActive = x.Assignment.IsActive,
                CreatedAt = x.Assignment.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<RolePermissionPolicyListItemResponse>.Create(items, pageIndex, pageSize, total);
    }

    private async Task<RolePermissionPolicy> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var assignment = await _unitOfWork.Repository<RolePermissionPolicy, Guid>()
            .Query()
            .FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, cancellationToken);

        if (assignment is null)
        {
            throw new NotFoundException(RolePermissionPolicyErrors.NotFound, "Role permission policy assignment not found.");
        }

        return assignment;
    }
}
