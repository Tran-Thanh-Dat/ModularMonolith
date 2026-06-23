using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Testing.Fakes;
using Identity.Application.Abstractions;
using Identity.Application.Auth.Login;
using Identity.Domain.Permissions;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.Application.Tests.Auth;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsInvalid_ReturnsInvalidCredentialsAndDoesNotCachePermissions()
    {
        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var activityLog = new FakeActivityLogService();
        var handler = CreateHandler(
            user: null,
            passwordValid: false,
            cacheBuffer: cacheBuffer,
            activityLog: activityLog);

        var result = await handler.Handle(
            new LoginCommand("unknown", "bad-password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.InvalidCredentials, result.Code);
        Assert.Empty(cacheBuffer.TakeAll());
        Assert.Equal(1, activityLog.LoginFailedCount);
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ReturnsTokenResponseAndEnqueuesPermissionCache()
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
        var activityLog = new FakeActivityLogService();
        var handler = CreateHandler(
            user: user,
            passwordValid: true,
            cacheBuffer: cacheBuffer,
            activityLog: activityLog);

        var result = await handler.Handle(
            new LoginCommand("admin", "password", false, "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Contains("Users.View", result.Data!.User.Permissions);
        Assert.Equal(1, activityLog.LoginSuccessCount);

        var operations = cacheBuffer.TakeAll();
        var operation = Assert.Single(operations);
        Assert.Equal(CacheKeys.UserPermissions(user.Id), operation.Key);
    }

    private static LoginCommandHandler CreateHandler(
        User? user,
        bool passwordValid,
        ICacheOperationBuffer cacheBuffer,
        FakeActivityLogService activityLog)
    {
        return new LoginCommandHandler(
            new FakeIdentityUserRepository(user),
            new FakeRefreshTokenRepository(),
            new FakePasswordHasher(passwordValid),
            new FakeJwtTokenService(),
            new FakeRefreshTokenService(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeRefreshTokenSettings(),
            new FakeAccessPolicyService(),
            activityLog,
            cacheBuffer,
            Options.Create(new CacheOptions { UserPermissionsExpirationMinutes = 20 }),
            NullLogger<LoginCommandHandler>.Instance);
    }

    private sealed class FakeAccessPolicyService : IAccessPolicyService
    {
        public Task<PasswordPolicyResponse> GetPasswordPolicyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new PasswordPolicyResponse());

        public Task UpdatePasswordPolicyAsync(UpdatePasswordPolicyRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<LoginPolicyResponse> GetLoginPolicyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoginPolicyResponse());

        public Task UpdateLoginPolicyAsync(UpdateLoginPolicyRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SessionPolicyResponse> GetSessionPolicyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SessionPolicyResponse
            {
                AccessTokenExpirationMinutes = 30,
                RefreshTokenExpirationDays = 7,
                RefreshTokenReuseDetectionEnabled = true
            });

        public Task UpdateSessionPolicyAsync(UpdateSessionPolicyRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<MaintenancePolicyResponse> GetMaintenancePolicyAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new MaintenancePolicyResponse());

        public Task UpdateMaintenancePolicyAsync(UpdateMaintenancePolicyRequest request, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

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

        public Task<User?> FindActiveByEmailAsync(
            string normalizedEmail,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<User?> FindActiveByIdForUpdateAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user);

        public Task<bool> EmailExistsForOtherUserAsync(
            string normalizedEmail,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
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

    private sealed class FakeJwtTokenService : IJwtTokenService
    {
        public Task<JwtTokenResult> GenerateAccessTokenAsync(
            User user,
            IReadOnlyCollection<string> roles,
            IReadOnlyCollection<string> permissions,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new JwtTokenResult
            {
                AccessToken = "access-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(30)
            });
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
