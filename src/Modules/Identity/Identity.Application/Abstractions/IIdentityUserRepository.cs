using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IIdentityUserRepository
{
    Task<User?> FindActiveByUserNameOrEmailAsync(
        string normalizedUserNameOrEmail,
        CancellationToken cancellationToken = default);

    Task<User?> FindActiveByIdWithRolesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
