using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Infrastructure.Caching;
using BuildingBlocks.Testing.Fakes;
using Identity.Application.Abstractions;
using Identity.Application.Auth.RefreshToken;
using Identity.Domain.RefreshTokens;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;
using Identity.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.Application.Tests.Auth;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRefreshTokenInvalid_ReturnsFailureAndDoesNotIssueAccessToken()
    {
        var jwtService = new TrackingJwtTokenService();
        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var handler = CreateHandler(
            storedToken: null,
            user: null,
            jwtService: jwtService,
            cacheBuffer: cacheBuffer);

        var result = await handler.Handle(
            new RefreshTokenCommand("invalid-token", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, result.Code);
        Assert.Equal(0, jwtService.GenerateCount);
        Assert.Empty(cacheBuffer.SetOperations);
    }

    [Fact]
    public async Task Handle_WhenRefreshTokenRevoked_DetectsReuseRevokesActiveTokensAndReturnsSafeFailure()
    {
        var userId = Guid.NewGuid();
        var storedToken = UserRefreshToken.Create(
            userId,
            "revoked-token",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            "127.0.0.1");
        storedToken.Revoke(DateTimeOffset.UtcNow, "127.0.0.1");

        var activeToken = UserRefreshToken.Create(
            userId,
            "active-token",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            "127.0.0.1");

        var refreshTokenRepository = new FakeRefreshTokenRepository(storedToken, activeToken);
        var activityLog = new FakeActivityLogService();
        var jwtService = new TrackingJwtTokenService();
        var handler = CreateHandler(
            storedToken: storedToken,
            user: User.Create("user", "user@example.com", "hash", "User", DateTimeOffset.UtcNow),
            jwtService: jwtService,
            refreshTokenRepository: refreshTokenRepository,
            activityLog: activityLog);

        var result = await handler.Handle(
            new RefreshTokenCommand("revoked-token", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, result.Code);
        Assert.Equal(0, jwtService.GenerateCount);
        Assert.Equal(1, refreshTokenRepository.RevokeAllCalls);
        Assert.NotNull(activeToken.RevokedAt);

        var reuseLog = Assert.Single(
            activityLog.ImmediateEntries,
            entry => entry.ActivityType == ActivityTypes.RefreshTokenReuseDetected);

        Assert.Equal(AuditLogStatus.Failed, reuseLog.Status);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsFailureAndDoesNotIssueAccessToken()
    {
        var userId = Guid.NewGuid();
        var storedToken = UserRefreshToken.Create(
            userId,
            "active-token",
            DateTimeOffset.UtcNow.AddDays(7),
            DateTimeOffset.UtcNow,
            "127.0.0.1");

        var jwtService = new TrackingJwtTokenService();
        var cacheBuffer = new RecordingCacheInvalidationBuffer();
        var handler = CreateHandler(
            storedToken: storedToken,
            user: null,
            jwtService: jwtService,
            cacheBuffer: cacheBuffer);

        var result = await handler.Handle(
            new RefreshTokenCommand("active-token", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AuthErrors.RefreshTokenInvalid, result.Code);
        Assert.Equal(0, jwtService.GenerateCount);
        Assert.Empty(cacheBuffer.SetOperations);
    }

    private static RefreshTokenCommandHandler CreateHandler(
        UserRefreshToken? storedToken,
        User? user,
        TrackingJwtTokenService? jwtService = null,
        RecordingCacheInvalidationBuffer? cacheBuffer = null,
        FakeRefreshTokenRepository? refreshTokenRepository = null,
        FakeActivityLogService? activityLog = null)
    {
        return new RefreshTokenCommandHandler(
            new FakeIdentityUserRepository(user),
            refreshTokenRepository ?? new FakeRefreshTokenRepository(storedToken),
            jwtService ?? new TrackingJwtTokenService(),
            new FakeRefreshTokenService(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeRefreshTokenSettings(),
            new FakeAccessPolicyService(),
            activityLog ?? new FakeActivityLogService(),
            cacheBuffer ?? new RecordingCacheInvalidationBuffer(),
            Options.Create(new CacheOptions { UserPermissionsExpirationMinutes = 20 }),
            NullLogger<RefreshTokenCommandHandler>.Instance);
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
        private readonly UserRefreshToken? _lookupToken;
        private readonly List<UserRefreshToken> _activeTokens;

        public FakeRefreshTokenRepository(UserRefreshToken? lookupToken, params UserRefreshToken[] activeTokens)
        {
            _lookupToken = lookupToken;
            _activeTokens = activeTokens.ToList();
        }

        public int RevokeAllCalls { get; private set; }

        public Task<UserRefreshToken?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_lookupToken);

        public Task AddAsync(UserRefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllActiveForUserAsync(
            Guid userId,
            DateTimeOffset revokedAt,
            string? revokedByIp = null,
            CancellationToken cancellationToken = default)
        {
            RevokeAllCalls++;

            foreach (var token in _activeTokens.Where(token => token.UserId == userId && token.RevokedAt is null))
            {
                token.Revoke(revokedAt, revokedByIp);
            }

            return Task.CompletedTask;
        }
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

    private sealed class TrackingJwtTokenService : IJwtTokenService
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
}
