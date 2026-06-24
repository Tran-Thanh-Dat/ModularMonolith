using Identity.Application.Abstractions;
using Identity.Domain.Users;

namespace Organizations.Infrastructure.Tests.Fakes;

internal sealed class FakeIdentityUserRepository : IIdentityUserRepository
{
    private readonly HashSet<Guid> _activeUserIds = [];

    public void AddActiveUser(Guid userId) => _activeUserIds.Add(userId);

    public Task<User?> FindActiveByUserNameOrEmailAsync(string normalizedUserNameOrEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> FindActiveByIdWithRolesAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> FindActiveByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<User?> FindActiveByIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_activeUserIds.Contains(userId))
        {
            return Task.FromResult<User?>(null);
        }

        return Task.FromResult<User?>(User.Create("testuser", "test@example.com", "hash", "Test User", DateTimeOffset.UtcNow));
    }

    public Task<bool> EmailExistsForOtherUserAsync(string normalizedEmail, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(false);
}
