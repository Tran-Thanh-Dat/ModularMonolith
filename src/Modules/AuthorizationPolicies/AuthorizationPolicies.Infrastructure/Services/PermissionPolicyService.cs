using AuditLogs.Application.Abstractions;
using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.Constants;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Domain.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.RolePermissionPolicies;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Services;

public sealed class PermissionPolicyService : IPermissionPolicyService
{
    private readonly AuthorizationPoliciesUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<PermissionPolicyService> _logger;

    public PermissionPolicyService(
        AuthorizationPoliciesUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<PermissionPolicyService> logger)
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
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        AuthorizationEffect effect,
        int priority,
        string? conditions,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = PermissionPolicy.NormalizeCode(code);
        var policies = _unitOfWork.Repository<PermissionPolicy, Guid>();

        if (await CodeExistsAsync(normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                PermissionPolicyErrors.CodeAlreadyExists,
                $"Permission policy code '{normalizedCode}' already exists.");
        }

        var policy = PermissionPolicy.Create(
            normalizedCode, name, description, permissionCode, moduleCode, resourceType, action,
            scope, effect, priority, conditions, _dateTimeProvider.UtcNow, _currentUserService.UserId);

        await policies.AddAsync(policy, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.PermissionPolicyCreated,
            $"Created permission policy: {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);

        _logger.LogInformation("Created permission policy {PolicyId} {Code}", policy.Id, policy.Code);
        return policy.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        string permissionCode,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        AuthorizationEffect effect,
        int priority,
        string? conditions,
        CancellationToken cancellationToken = default)
    {
        var policy = await GetEntityAsync(id, cancellationToken);
        policy.Update(name, description, permissionCode, moduleCode, resourceType, action,
            scope, effect, priority, conditions, _currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.PermissionPolicyUpdated,
            $"Updated permission policy: {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetEntityAsync(id, cancellationToken);

        var hasRoleAssignments = await _unitOfWork.Repository<RolePermissionPolicy, Guid>()
            .Query()
            .AnyAsync(r => r.PermissionPolicyId == id && !r.IsDeleted, cancellationToken);

        var hasUserOverrides = await _unitOfWork.Repository<UserPermissionPolicyOverride, Guid>()
            .Query()
            .AnyAsync(u => u.PermissionPolicyId == id && !u.IsDeleted, cancellationToken);

        if (hasRoleAssignments || hasUserOverrides)
        {
            throw new BadRequestException(
                PermissionPolicyErrors.HasAssignments,
                "Cannot delete permission policy while role or user assignments exist.");
        }

        policy.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.PermissionPolicyDeleted,
            $"Deleted permission policy: {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetEntityAsync(id, cancellationToken);
        policy.Activate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.PermissionPolicyActivated,
            $"Activated permission policy: {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await GetEntityAsync(id, cancellationToken);
        policy.Deactivate();

        await _activityLogService.EnqueuePostCommitAsync(
            AuthorizationPoliciesActivityTypes.PermissionPolicyDeactivated,
            $"Deactivated permission policy: {policy.Code}",
            AuthorizationPoliciesModuleConstants.ModuleName,
            cancellationToken: cancellationToken);
    }

    public async Task<PermissionPolicyDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        return policy is null ? null : MapDetail(policy);
    }

    public async Task<PagedResult<PermissionPolicyListItemResponse>> GetListAsync(
        string? keyword,
        string? permissionCode,
        string? moduleCode,
        string? resourceType,
        string? action,
        AuthorizationScope? scope,
        AuthorizationEffect? effect,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(p => p.Code.Contains(k) || p.Name.Contains(k));
        }

        if (!string.IsNullOrWhiteSpace(permissionCode))
        {
            query = query.Where(p => p.PermissionCode == permissionCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(moduleCode))
        {
            query = query.Where(p => p.ModuleCode == moduleCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(p => p.ResourceType == resourceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(action))
        {
            query = query.Where(p => p.Action == action.Trim());
        }

        if (scope.HasValue)
        {
            query = query.Where(p => p.Scope == scope.Value);
        }

        if (effect.HasValue)
        {
            query = query.Where(p => p.Effect == effect.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(p => MapListItem(p))
            .ToListAsync(cancellationToken);

        return PagedResult<PermissionPolicyListItemResponse>.Create(items, pageIndex, pageSize, total);
    }

    private async Task<PermissionPolicy> GetEntityAsync(Guid id, CancellationToken cancellationToken)
    {
        var policy = await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException(PermissionPolicyErrors.NotFound, "Permission policy not found.");
        }

        return policy;
    }

    private async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<PermissionPolicy, Guid>()
            .Query()
            .AnyAsync(p => !p.IsDeleted && p.Code == code && (!excludeId.HasValue || p.Id != excludeId.Value), cancellationToken);

    private static PermissionPolicyListItemResponse MapListItem(PermissionPolicy p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        PermissionCode = p.PermissionCode,
        ModuleCode = p.ModuleCode,
        ResourceType = p.ResourceType,
        Action = p.Action,
        Scope = p.Scope,
        Effect = p.Effect,
        Priority = p.Priority,
        IsActive = p.IsActive,
        CreatedAt = p.CreatedAt
    };

    private static PermissionPolicyDetailResponse MapDetail(PermissionPolicy p) => new()
    {
        Id = p.Id,
        Code = p.Code,
        Name = p.Name,
        Description = p.Description,
        PermissionCode = p.PermissionCode,
        ModuleCode = p.ModuleCode,
        ResourceType = p.ResourceType,
        Action = p.Action,
        Scope = p.Scope,
        Effect = p.Effect,
        Priority = p.Priority,
        IsActive = p.IsActive,
        Conditions = p.Conditions,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
