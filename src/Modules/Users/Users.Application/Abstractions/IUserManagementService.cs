using BuildingBlocks.Application.Pagination;
using Users.Application.Users.CreateUser;
using Users.Application.Users.GetUserById;
using Users.Application.Users.GetUsers;

namespace Users.Application.Abstractions;

public interface IUserManagementService
{
    Task<PagedResult<UserListItemResponse>> GetUsersAsync(
        string? keyword,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<UserDetailResponse?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateUserAsync(
        string userName,
        string email,
        string fullName,
        string password,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);

    Task UpdateUserAsync(
        Guid userId,
        string email,
        string fullName,
        CancellationToken cancellationToken = default);

    Task ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AssignRolesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task AssignPermissionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default);

    Task RemovePermissionAsync(
        Guid userId,
        Guid permissionId,
        CancellationToken cancellationToken = default);
}
