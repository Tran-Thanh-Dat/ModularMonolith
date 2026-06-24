using AuditLogs.Application.Abstractions;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Identity.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class UserPermissionPolicyOverrideService : IUserPermissionPolicyOverrideService
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly ILogger<UserPermissionPolicyOverrideService> _logger;

    public UserPermissionPolicyOverrideService(
        AuthorizationPoliciesUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        IIdentityUserRepository identityUserRepository,
        ILogger<UserPermissionPolicyOverrideService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _identityUserRepository = identityUserRepository;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        Guid userId,
        Guid permissionPolicyId,
        AuthorizationEffect effect,
        DateTimeOffset? expiresAt,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        if (expiresAt.HasValue && expiresAt.Value <= _dateTimeProvider.UtcNow)
        {
            throw new BadRequestException(
                UserPermissionPolicyOverrideErrors.InvalidExpiresAt,
                "ExpiresAt must be in the future.");
        }

        var user = await _identityUserRepository.FindActiveByIdWithRolesAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new NotFoundException(UserErrors.NotFound, "User not found.");
        }

        var policy = await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .FirstOrDefaultAsync(p => p.Id == permissionPolicyId && !p.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(PermissionPolicyErrors.NotFound, "Permission policy not found.");

        if (!policy.IsActive)
        {
            throw new BadRequestException(UserPermissionPolicyOverrideErrors.PolicyInactive, "Cannot assign inactive permission policy.");
        }

        var repo = _unitOfWork.Repository<UserPermissionPolicyOverride, Guid>();
        if (await repo.Query().AnyAsync(u => !u.IsDeleted && u.UserId == userId && u.PermissionPolicyId == permissionPolicyId, cancellationToken))
        {
            throw new ConflictException(UserPermissionPolicyOverrideErrors.AlreadyExists, "User permission policy override already exists.");
        }

        var item = UserPermissionPolicyOverride.Create(
            userId, permissionPolicyId, effect, expiresAt, reason, _dateTimeProvider.UtcNow, _currentUserService.UserId);
        await repo.AddAsync(item, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.UserPermissionPolicyOverrideCreated,
            $"Created user override for policy {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created user override {OverrideId} for user {UserId}", item.Id, userId);
        return item.Id;
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetEntityAsync(id, cancellationToken);
        item.Activate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.UserPermissionPolicyOverrideActivated,
            $"Activated user permission policy override {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetEntityAsync(id, cancellationToken);
        item.Deactivate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.UserPermissionPolicyOverrideDeactivated,
            $"Deactivated user permission policy override {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await GetEntityAsync(id, cancellationToken);
        item.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.UserPermissionPolicyOverrideRemoved,
            $"Removed user permission policy override {id}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<UserPermissionPolicyOverrideListItemResponse>> GetListAsync(
        Guid? userId,
        Guid? permissionPolicyId,
        AuthorizationEffect? effect,
        bool? isActive,
        bool includeExpired,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var query = from o in _unitOfWork.Repository<UserPermissionPolicyOverride, Guid>().Query().AsNoTracking()
                    join p in _unitOfWork.Repository<PermissionPolicy, Guid>().Query().AsNoTracking()
                        on o.PermissionPolicyId equals p.Id
                    where !o.IsDeleted && !p.IsDeleted
                    select new { Override = o, Policy = p };

        if (userId.HasValue)
        {
            query = query.Where(x => x.Override.UserId == userId.Value);
        }

        if (permissionPolicyId.HasValue)
        {
            query = query.Where(x => x.Override.PermissionPolicyId == permissionPolicyId.Value);
        }

        if (effect.HasValue)
        {
            query = query.Where(x => x.Override.Effect == effect.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(x => x.Override.IsActive == isActive.Value);
        }

        if (!includeExpired)
        {
            query = query.Where(x => !x.Override.ExpiresAt.HasValue || x.Override.ExpiresAt > now);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Override.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new UserPermissionPolicyOverrideListItemResponse
            {
                Id = x.Override.Id,
                UserId = x.Override.UserId,
                PermissionPolicyId = x.Override.PermissionPolicyId,
                PermissionPolicyCode = x.Policy.Code,
                PermissionCode = x.Policy.PermissionCode,
                Effect = x.Override.Effect,
                IsActive = x.Override.IsActive,
                ExpiresAt = x.Override.ExpiresAt,
                Reason = x.Override.Reason,
                CreatedAt = x.Override.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<UserPermissionPolicyOverrideListItemResponse>.Create(items, pageIndex, pageSize, total);
    }

    private async Task<UserPermissionPolicyOverride> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await _unitOfWork.Repository<UserPermissionPolicyOverride, Guid>()
            .Query()
            .FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted, cancellationToken);

        if (item is null)
        {
            throw new NotFoundException(UserPermissionPolicyOverrideErrors.NotFound, "User permission policy override not found.");
        }

        return item;
    }
}
