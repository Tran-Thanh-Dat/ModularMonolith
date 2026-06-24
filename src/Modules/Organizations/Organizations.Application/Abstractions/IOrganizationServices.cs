using BuildingBlocks.Application.Pagination;
using Organizations.Application.Tenants;
using Organizations.Application.Organizations;
using Organizations.Application.OrganizationUsers;
using Organizations.Application.Workspaces;
using Organizations.Application.WorkspaceUsers;
using Organizations.Domain.Enums;

namespace Organizations.Application.Abstractions;

public interface ITenantService
{
    Task<Guid> CreateAsync(string code, string name, string? description, string? metadata, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, string? description, string? metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TenantListItemResponse>> GetListAsync(string? keyword, bool? isActive, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

public interface IOrganizationService
{
    Task<Guid> CreateAsync(Guid tenantId, Guid? parentOrganizationId, string code, string name, string? description, OrganizationType type, int sortOrder, string? metadata, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, string? description, OrganizationType type, int sortOrder, string? metadata, Guid? parentOrganizationId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<OrganizationListItemResponse>> GetListAsync(Guid? tenantId, Guid? parentOrganizationId, string? keyword, bool? isActive, OrganizationType? type, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationTreeNodeResponse>> GetTreeAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IOrganizationUserService
{
    Task<Guid> AssignAsync(Guid tenantId, Guid organizationId, Guid userId, bool isDefault, CancellationToken cancellationToken = default);
    Task SetDefaultAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrganizationUserDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<OrganizationUserListItemResponse>> GetListAsync(Guid? tenantId, Guid? organizationId, Guid? userId, bool? isActive, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationUserListItemResponse>> GetUserOrganizationsAsync(Guid userId, Guid? tenantId, CancellationToken cancellationToken = default);
}

public interface IWorkspaceService
{
    Task<Guid> CreateAsync(Guid tenantId, Guid? organizationId, string code, string name, string? description, string? metadata, CancellationToken cancellationToken = default);
    Task UpdateAsync(Guid id, string name, string? description, Guid? organizationId, string? metadata, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkspaceListItemResponse>> GetListAsync(Guid? tenantId, Guid? organizationId, string? keyword, bool? isActive, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}

public interface IWorkspaceUserService
{
    Task<Guid> AssignAsync(Guid tenantId, Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
    Task RemoveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkspaceUserDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<WorkspaceUserListItemResponse>> GetListAsync(Guid? tenantId, Guid? workspaceId, Guid? userId, bool? isActive, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
}
