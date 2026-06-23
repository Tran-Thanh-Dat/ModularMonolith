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

    Task<User?> FindActiveByEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<User?> FindActiveByIdForUpdateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsForOtherUserAsync(
        string normalizedEmail,
        Guid userId,
        CancellationToken cancellationToken = default);
}
