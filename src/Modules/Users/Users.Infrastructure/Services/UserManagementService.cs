using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Identity.Application.Abstractions;
using Identity.Domain.Constants;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Users.Application.Abstractions;
using Users.Application.Users.GetUserById;
using Users.Application.Users.GetUsers;

namespace Users.Infrastructure.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IdentityUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUserService;
    private readonly IActivityLogService _activityLogService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        IdentityUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ICurrentUserService currentUserService,
        IActivityLogService activityLogService,
        IDateTimeProvider dateTimeProvider,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<UserManagementService> logger)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _currentUserService = currentUserService;
        _activityLogService = activityLogService;
        _dateTimeProvider = dateTimeProvider;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<PagedResult<UserListItemResponse>> GetUsersAsync(
        string? keyword,
        bool? isActive,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest
        {
            PageIndex = pageNumber,
            PageSize = pageSize,
            Keyword = keyword
        };

        var query = _unitOfWork.Repository<User, Guid>()
            .QueryReadOnly()
            .Include(user => user.Roles)
            .Where(user => !user.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(user =>
                EF.Functions.ILike(user.UserName, pattern) ||
                EF.Functions.ILike(user.Email, pattern) ||
                EF.Functions.ILike(user.FullName, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(user => user.IsActive == isActive.Value);
        }

        var projected = query
            .OrderBy(user => user.UserName)
            .Select(user => new UserListItemResponse
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt,
                Roles = user.Roles.Where(role => role.IsActive).Select(role => role.Code).ToArray(),
                CreatedAt = user.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<UserDetailResponse?> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.Repository<User, Guid>()
            .QueryReadOnly()
            .Include(u => u.Roles)
            .ThenInclude(r => r.Permissions)
            .Include(u => u.DirectPermissions)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return MapUserDetail(user);
    }

    public async Task<Guid> CreateUserAsync(
        string userName,
        string email,
        string fullName,
        string password,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var normalizedUserName = userName.Trim();
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var users = _unitOfWork.Repository<User, Guid>();

        if (await users.QueryReadOnly().AnyAsync(
                u => !u.IsDeleted && u.UserName == normalizedUserName,
                cancellationToken))
        {
            _logger.LogWarning("Create user failed due to duplicate username {UserName}", normalizedUserName);
            throw new ConflictException(
                UserErrors.UserNameAlreadyExists,
                $"Username '{normalizedUserName}' is already taken.");
        }

        if (await users.QueryReadOnly().AnyAsync(
                u => !u.IsDeleted && u.Email == normalizedEmail,
                cancellationToken))
        {
            _logger.LogWarning("Create user failed due to duplicate email {Email}", normalizedEmail);
            throw new ConflictException(
                UserErrors.EmailAlreadyExists,
                $"Email '{normalizedEmail}' is already taken.");
        }

        var roles = await LoadRolesByIdsAsync(roleIds, cancellationToken);
        ValidateAllRolesFound(roleIds, roles);

        var passwordHash = _passwordHasher.HashPassword(password);
        var user = User.Create(
            normalizedUserName,
            normalizedEmail,
            passwordHash,
            fullName.Trim(),
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        foreach (var role in roles)
        {
            user.AssignRole(role);
        }

        await users.AddAsync(user, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"User '{user.UserName}' was created.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Created user {UserId} {UserName} with {RoleCount} roles",
            user.Id,
            user.UserName,
            roles.Count);

        return user.Id;
    }

    public async Task UpdateUserAsync(
        Guid userId,
        string email,
        string fullName,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken);
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase) &&
            await _unitOfWork.Repository<User, Guid>().QueryReadOnly().AnyAsync(
                u => !u.IsDeleted && u.Email == normalizedEmail && u.Id != userId,
                cancellationToken))
        {
            _logger.LogWarning("Update user failed due to duplicate email {Email} for {UserId}", normalizedEmail, userId);
            throw new ConflictException(
                UserErrors.EmailAlreadyExists,
                $"Email '{normalizedEmail}' is already taken.");
        }

        user.UpdateProfile(fullName.Trim(), normalizedEmail);
        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"User '{user.UserName}' profile was updated.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);
        _logger.LogInformation("Updated user profile for {UserId}", userId);
    }

    public async Task ActivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken);

        if (!user.IsActive)
        {
            user.Activate();
            await _activityLogService.EnqueuePostCommitAsync(
                ActivityTypes.Activate,
                $"User '{user.UserName}' was activated.",
                AuditLogConstants.Modules.Users,
                cancellationToken: cancellationToken);
            _logger.LogInformation("Activated user {UserId}", userId);
        }
    }

    public async Task DeactivateUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService.UserId == userId)
        {
            _logger.LogWarning("Prevented self-deactivation for {UserId}", userId);
            throw new ConflictException(
                UserErrors.CannotDeleteSelf,
                "You cannot deactivate your own account.");
        }

        var user = await GetUserForUpdateAsync(userId, cancellationToken, includeRoles: true);

        if (user.IsActive)
        {
            await EnsureNotLastActiveAdminAsync(user, cancellationToken);
            user.Deactivate();
            await _activityLogService.EnqueuePostCommitAsync(
                ActivityTypes.Deactivate,
                $"User '{user.UserName}' was deactivated.",
                AuditLogConstants.Modules.Users,
                cancellationToken: cancellationToken);
            InvalidateUserPermissionsCache(userId);
            _logger.LogInformation("Deactivated user {UserId}", userId);
        }
    }

    public async Task AssignRolesAsync(
        Guid userId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken, includeRoles: true);
        var newRoles = await LoadRolesByIdsAsync(roleIds, cancellationToken);
        ValidateAllRolesFound(roleIds, newRoles);

        var adminRole = await GetAdminRoleAsync(cancellationToken);
        if (adminRole is not null)
        {
            var hadAdmin = user.Roles.Any(role => role.Id == adminRole.Id);
            var willHaveAdmin = newRoles.Any(role => role.Id == adminRole.Id);

            if (hadAdmin && !willHaveAdmin)
            {
                await EnsureNotLastActiveAdminAsync(user, cancellationToken);
            }
        }

        user.Roles.Clear();
        foreach (var role in newRoles)
        {
            user.AssignRole(role);
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.AssignRole,
            $"Roles were assigned to user '{user.UserName}'.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);

        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation(
            "Replaced roles for user {UserId} with {RoleCount} roles",
            userId,
            newRoles.Count);
    }

    public async Task RemoveRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken, includeRoles: true);
        var role = user.Roles.FirstOrDefault(existingRole => existingRole.Id == roleId);

        if (role is null)
        {
            return;
        }

        var adminRole = await GetAdminRoleAsync(cancellationToken);
        if (adminRole is not null && role.Id == adminRole.Id)
        {
            await EnsureNotLastActiveAdminAsync(user, cancellationToken);
        }

        user.RemoveRole(role);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.RemoveRole,
            $"Role '{role.Code}' was removed from user '{user.UserName}'.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);

        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation("Removed role {RoleId} from user {UserId}", roleId, userId);
    }

    public async Task AssignPermissionsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken, includeDirectPermissions: true);
        var permissions = await _unitOfWork.Repository<Permission, Guid>()
            .QueryReadOnly()
            .Where(permission => permissionIds.Contains(permission.Id))
            .ToListAsync(cancellationToken);

        if (permissions.Count != permissionIds.Count)
        {
            throw new NotFoundException(
                PermissionErrors.NotFound,
                "One or more permission ids were not found.");
        }

        user.DirectPermissions.Clear();
        foreach (var permission in permissions)
        {
            user.AssignDirectPermission(permission);
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.AssignPermission,
            $"Direct permissions were assigned to user '{user.UserName}'.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);

        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation(
            "Replaced direct permissions for user {UserId} with {PermissionCount} permissions",
            userId,
            permissions.Count);
    }

    public async Task RemovePermissionAsync(
        Guid userId,
        Guid permissionId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserForUpdateAsync(userId, cancellationToken, includeDirectPermissions: true);
        var permission = user.DirectPermissions.FirstOrDefault(existing => existing.Id == permissionId);

        if (permission is null)
        {
            return;
        }

        user.RemoveDirectPermission(permission);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.RemovePermission,
            $"Direct permission '{permission.Code}' was removed from user '{user.UserName}'.",
            AuditLogConstants.Modules.Users,
            cancellationToken: cancellationToken);

        InvalidateUserPermissionsCache(userId);

        _logger.LogInformation(
            "Removed direct permission {PermissionId} from user {UserId}",
            permissionId,
            userId);
    }

    private async Task<User> GetUserForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken,
        bool includeRoles = false,
        bool includeDirectPermissions = false)
    {
        IQueryable<User> query = _unitOfWork.Repository<User, Guid>().Query();

        if (includeRoles)
        {
            query = query.Include(user => user.Roles);
        }

        if (includeDirectPermissions)
        {
            query = query.Include(user => user.DirectPermissions);
        }

        var user = await query.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException(
                UserErrors.NotFound,
                $"User with id '{userId}' was not found.");
        }

        return user;
    }

    private async Task<List<Role>> LoadRolesByIdsAsync(
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return [];
        }

        return await _unitOfWork.Repository<Role, Guid>()
            .QueryReadOnly()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);
    }

    private static void ValidateAllRolesFound(IReadOnlyCollection<Guid> roleIds, IReadOnlyCollection<Role> roles)
    {
        if (roles.Count != roleIds.Count)
        {
            throw new NotFoundException(
                RoleErrors.NotFound,
                "One or more role ids were not found.");
        }
    }

    private async Task<Role?> GetAdminRoleAsync(CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<Role, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(role => role.Code == IdentityConstants.Roles.Admin, cancellationToken);

    private async Task EnsureNotLastActiveAdminAsync(User user, CancellationToken cancellationToken)
    {
        var adminRole = await GetAdminRoleAsync(cancellationToken);
        if (adminRole is null)
        {
            return;
        }

        var userIsAdmin = user.Roles.Any(role => role.Id == adminRole.Id);
        if (!userIsAdmin || !user.IsActive)
        {
            return;
        }

        var otherActiveAdmins = await _unitOfWork.Repository<User, Guid>()
            .QueryReadOnly()
            .Where(u => !u.IsDeleted && u.IsActive && u.Id != user.Id)
            .Where(u => u.Roles.Any(role => role.Id == adminRole.Id))
            .CountAsync(cancellationToken);

        if (otherActiveAdmins == 0)
        {
            _logger.LogWarning(
                "Prevented modification of last active admin {UserId}",
                user.Id);

            throw new ConflictException(
                RoleErrors.PermissionInvalid,
                "Cannot modify the last active administrator.");
        }
    }

    private static UserDetailResponse MapUserDetail(User user)
    {
        var rolePermissions = user.Roles
            .Where(role => role.IsActive)
            .SelectMany(role => role.Permissions)
            .Select(permission => new PermissionResponse
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                Module = permission.Module
            });

        var directPermissions = user.DirectPermissions
            .Select(permission => new PermissionResponse
            {
                Id = permission.Id,
                Code = permission.Code,
                Name = permission.Name,
                Module = permission.Module
            });

        var permissions = rolePermissions
            .Concat(directPermissions)
            .GroupBy(permission => permission.Id)
            .Select(group => group.First())
            .OrderBy(permission => permission.Code)
            .ToArray();

        return new UserDetailResponse
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            IsActive = user.IsActive,
            LastLoginAt = user.LastLoginAt,
            Roles = user.Roles
                .Where(role => role.IsActive)
                .Select(role => new RoleResponse
                {
                    Id = role.Id,
                    Code = role.Code,
                    Name = role.Name
                })
                .ToArray(),
            Permissions = permissions,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private void InvalidateUserPermissionsCache(Guid userId) =>
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.UserPermissions(userId));
}
