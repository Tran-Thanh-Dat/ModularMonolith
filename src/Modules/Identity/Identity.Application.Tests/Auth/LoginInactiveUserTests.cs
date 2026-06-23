using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Testing.Fakes;
using Identity.Application.Abstractions;
using Identity.Application.Auth.Login;
using Identity.Domain.Permissions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.Application.Tests.Auth;

public sealed class LoginInactiveUserTests
{
    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsInvalidCredentialsAndDoesNotCachePermissions()
    {
        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var jwtService = new TrackingJwtTokenService();
        var handler = LoginTestSupport.CreateHandler(
            user: null,
            passwordValid: true,
            cacheBuffer: cacheBuffer,
            jwtService: jwtService);

        var result = await handler.Handle(
            new LoginCommand("inactive-user", "password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Code);
        Assert.Equal(0, jwtService.GenerateCount);
        Assert.Empty(cacheBuffer.SetOperations);
    }
}

public sealed class LoginPermissionCachePostCommitTests
{
    [Fact]
    public async Task Handle_WhenLoginSucceeds_EnqueuesCacheButDoesNotWriteUntilPostCommitHook()
    {
        var role = Role.Create("Admin", "Administrator");
        role.AddPermission(Permission.Create("Users.View", "Users.View", "Users"));
        var user = User.Create(
            "admin",
            "admin@example.com",
            "hash",
            "Admin User",
            DateTimeOffset.UtcNow);
        user.AssignRole(role);

        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var cache = new RecordingCacheService();
        var handler = LoginTestSupport.CreateHandler(user, cacheBuffer);

        var result = await handler.Handle(
            new LoginCommand("admin", "password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(cacheBuffer.SetOperations);
        Assert.Equal(CacheKeys.UserPermissions(user.Id), cacheBuffer.SetOperations[0].Key);
        Assert.Empty(cache.SetOperations);

        var hook = new CacheInvalidationPostCommitHook(
            cacheBuffer,
            cache,
            NullLogger<CacheInvalidationPostCommitHook>.Instance);

        await hook.OnCommittedAsync();

        Assert.Single(cache.SetOperations);
        Assert.Equal(CacheKeys.UserPermissions(user.Id), cache.SetOperations[0].Key);
    }

    [Fact]
    public async Task Handle_WhenLoginFails_DoesNotEnqueueOrWritePermissionCache()
    {
        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var cache = new RecordingCacheService();
        var handler = LoginTestSupport.CreateHandler(null, cacheBuffer);

        var result = await handler.Handle(
            new LoginCommand("unknown", "bad-password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(cacheBuffer.SetOperations);

        var hook = new CacheInvalidationPostCommitHook(
            cacheBuffer,
            cache,
            NullLogger<CacheInvalidationPostCommitHook>.Instance);
        await hook.OnCommittedAsync();

        Assert.Empty(cache.SetOperations);
    }
}

internal static class LoginTestSupport
{
    public static LoginCommandHandler CreateHandler(
        User? user,
        RecordingCacheInvalidationBuffer cacheBuffer,
        TrackingJwtTokenService? jwtService = null) =>
        new(
            new FakeIdentityUserRepository(user),
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(user is not null),
            jwtService ?? new TrackingJwtTokenService(),
            new FakeRefreshTokenService(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeRefreshTokenSettings(),
            new FakeActivityLogService(),
            cacheBuffer,
            Options.Create(new CacheOptions { UserPermissionsExpirationMinutes = 20 }),
            NullLogger<LoginCommandHandler>.Instance);

    public static LoginCommandHandler CreateHandler(
        User? user,
        bool passwordValid,
        RecordingCacheInvalidationBuffer cacheBuffer,
        TrackingJwtTokenService jwtService) =>
        new(
            new FakeIdentityUserRepository(user),
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(passwordValid),
            jwtService,
            new FakeRefreshTokenService(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeRefreshTokenSettings(),
            new FakeActivityLogService(),
            cacheBuffer,
            Options.Create(new CacheOptions { UserPermissionsExpirationMinutes = 20 }),
            NullLogger<LoginCommandHandler>.Instance);

    private sealed class FakeIdentityUserRepository(User? user) : IIdentityUserRepository
    {
        public Task<User?> FindActiveByUserNameOrEmailAsync(
            string normalizedUserNameOrEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<User?> FindActiveByIdWithRolesAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);
    }

    private sealed class FakeRefreshTokenRepository : IIdentityRefreshTokenRepository
    {
        public Task<Identity.Domain.RefreshTokens.UserRefreshToken?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Identity.Domain.RefreshTokens.UserRefreshToken?>(null);

        public Task AddAsync(
            Identity.Domain.RefreshTokens.UserRefreshToken refreshToken,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllActiveForUserAsync(
            Guid userId,
            DateTimeOffset revokedAt,
            string? revokedByIp = null,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePasswordHasher(bool valid) : IPasswordHasher
    {
        public string HashPassword(string password) => password;

        public bool VerifyPassword(string password, string passwordHash) => valid;
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        public string GenerateRefreshToken() => "refresh-token";

        public string HashRefreshToken(string refreshToken) => refreshToken;

        public bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
            refreshToken == refreshTokenHash;
    }

    private sealed class FakeRefreshTokenSettings : IRefreshTokenSettings
    {
        public int ExpirationDays => 7;
    }
}

internal sealed class TrackingJwtTokenService : IJwtTokenService
{
    public int GenerateCount { get; private set; }

    public Task<JwtTokenResult> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default)
    {
        GenerateCount++;
        return Task.FromResult(new JwtTokenResult
        {
            AccessToken = "access-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
        });
    }
}
