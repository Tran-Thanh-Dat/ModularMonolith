using AuthorizationPolicies.Application.Models;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;

namespace AuthorizationPolicies.Application.Abstractions;

public interface IPermissionPolicyService
{
    Task<Guid> CreateAsync(
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
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
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
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PermissionPolicyDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PermissionPolicyListItemResponse>> GetListAsync(
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
        CancellationToken cancellationToken = default);
}

public interface IRolePermissionPolicyService
{
    Task<Guid> AssignAsync(Guid roleId, Guid permissionPolicyId, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<RolePermissionPolicyListItemResponse>> GetListAsync(
        Guid? roleId,
        Guid? permissionPolicyId,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IUserPermissionPolicyOverrideService
{
    Task<Guid> CreateAsync(
        Guid userId,
        Guid permissionPolicyId,
        AuthorizationEffect effect,
        DateTimeOffset? expiresAt,
        string? reason,
        CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<UserPermissionPolicyOverrideListItemResponse>> GetListAsync(
        Guid? userId,
        Guid? permissionPolicyId,
        AuthorizationEffect? effect,
        bool? isActive,
        bool includeExpired,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IAuthorizationMatrixEntryService
{
    Task<Guid> CreateAsync(
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string moduleCode,
        string resourceType,
        string action,
        AuthorizationScope scope,
        string requiredPermissionCode,
        string? description,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task EnableAsync(Guid id, CancellationToken cancellationToken = default);
    Task DisableAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuthorizationMatrixEntryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<AuthorizationMatrixEntryListItemResponse>> GetListAsync(
        string? moduleCode,
        string? resourceType,
        string? action,
        AuthorizationScope? scope,
        string? requiredPermissionCode,
        bool? isEnabled,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface ICurrentUserPermissionContextService
{
    Task<CurrentUserPermissionContext> GetContextAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IAuthorizationMatrixService
{
    Task<Result<AuthorizationDecisionResponse>> CheckAsync(
        Guid userId,
        string? permissionCode,
        IAuthorizationResourceContext resourceContext,
        string? action = null,
        string? resourceType = null,
        string? moduleCode = null,
        CancellationToken cancellationToken = default);

    Task<Result> AuthorizeAsync(
        CurrentUserPermissionContext userContext,
        string requiredPermission,
        IAuthorizationResourceContext resourceContext,
        AuthorizationScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EffectivePolicyInfo>> GetEffectivePoliciesAsync(
        Guid userId,
        string? moduleCode = null,
        string? resourceType = null,
        string? action = null,
        CancellationToken cancellationToken = default);

    Task<Result<AuthorizationExplanationResponse>> ExplainAsync(
        Guid userId,
        string? permissionCode,
        IAuthorizationResourceContext resourceContext,
        string? action = null,
        string? resourceType = null,
        string? moduleCode = null,
        CancellationToken cancellationToken = default);
}

public interface IAuthorizationPoliciesSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
