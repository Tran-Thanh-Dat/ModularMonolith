using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Testing.Fakes;
using Identity.Application.Abstractions;
using Identity.Application.Account.ChangePassword;
using Identity.Application.Account.ForgotPassword;
using Identity.Application.Account.ResetPassword;
using Identity.Domain.PasswordResetTokens;
using Identity.Domain.RefreshTokens;
using Identity.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Identity.Application.Tests.Account;

public sealed class ChangePasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCurrentPasswordInvalid_ReturnsFailure()
    {
        var user = CreateUser();
        var handler = CreateChangePasswordHandler(user, currentPasswordValid: false);

        var result = await handler.Handle(
            new ChangePasswordCommand("wrong", "NewPass@123", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.CurrentPasswordInvalid, result.Code);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordValid_ChangesPasswordAndRevokesRefreshTokens()
    {
        var user = CreateUser();
        var refreshRepo = new RecordingRefreshTokenRepository();
        var handler = CreateChangePasswordHandler(user, currentPasswordValid: true, refreshRepo);

        var result = await handler.Handle(
            new ChangePasswordCommand("old", "NewPass@123", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, refreshRepo.RevokeAllCalls);
    }

    [Fact]
    public async Task Handle_WhenResetTokenInvalid_ReturnsFailure()
    {
        var handler = CreateResetPasswordHandler(user: null, resetToken: null);

        var result = await handler.Handle(
            new ResetPasswordCommand("bad-token", "NewPass@123", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(AccountErrors.PasswordResetTokenInvalid, result.Code);
    }

    [Fact]
    public async Task Handle_WhenResetTokenValid_ResetsPassword()
    {
        var user = CreateUser();
        var rawToken = "reset-token";
        var refreshService = new FakeRefreshTokenService();
        var tokenHash = refreshService.HashRefreshToken(rawToken);
        var resetToken = PasswordResetToken.Create(
            user.Id,
            tokenHash,
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow);

        var handler = CreateResetPasswordHandler(user, resetToken, refreshService);

        var result = await handler.Handle(
            new ResetPasswordCommand(rawToken, "NewPass@123", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(resetToken.UsedAt);
    }

    [Fact]
    public async Task Handle_WhenEmailUnknown_ReturnsGenericSuccess()
    {
        var handler = CreateForgotPasswordHandler(user: null);

        var result = await handler.Handle(
            new ForgotPasswordCommand("missing@example.com", "127.0.0.1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
    }

    private static User CreateUser() =>
        User.Create(
            "demo",
            "demo@example.com",
            "hash",
            "Demo User",
            DateTimeOffset.UtcNow);

    private static ChangePasswordCommandHandler CreateChangePasswordHandler(
        User user,
        bool currentPasswordValid,
        RecordingRefreshTokenRepository? refreshRepo = null)
    {
        return new ChangePasswordCommandHandler(
            new FakeCurrentUserService(user.Id, "demo"),
            new FakeIdentityUserRepository(user),
            new FakePasswordHasher(currentPasswordValid),
            refreshRepo ?? new RecordingRefreshTokenRepository(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeActivityLogService(),
            NullLogger<ChangePasswordCommandHandler>.Instance);
    }

    private static ResetPasswordCommandHandler CreateResetPasswordHandler(
        User? user,
        PasswordResetToken? resetToken,
        FakeRefreshTokenService? refreshService = null)
    {
        refreshService ??= new FakeRefreshTokenService();

        return new ResetPasswordCommandHandler(
            new FakePasswordResetTokenRepository(resetToken, refreshService),
            refreshService,
            new FakePasswordHasher(valid: true),
            new RecordingRefreshTokenRepository(),
            new FakeIdentityUserRepository(user),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            new FakeActivityLogService(),
            NullLogger<ResetPasswordCommandHandler>.Instance);
    }

    private static ForgotPasswordCommandHandler CreateForgotPasswordHandler(User? user)
    {
        return new ForgotPasswordCommandHandler(
            new FakeIdentityUserRepository(user),
            new FakePasswordResetTokenRepository(null, new FakeRefreshTokenService()),
            new FakeRefreshTokenService(),
            new FakePasswordResetSettings(),
            new FakeAccountEmailService(),
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<ForgotPasswordCommandHandler>.Instance);
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
            Task.FromResult(
                user is not null && user.Email.Equals(normalizedEmail, StringComparison.OrdinalIgnoreCase)
                    ? user
                    : null);

        public Task<User?> FindActiveByIdForUpdateAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(user?.Id == userId ? user : null);

        public Task<bool> EmailExistsForOtherUserAsync(
            string normalizedEmail,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class FakePasswordHasher(bool valid) : IPasswordHasher
    {
        public string HashPassword(string password) => $"hashed:{password}";

        public bool VerifyPassword(string password, string passwordHash) => valid;
    }

    private sealed class RecordingRefreshTokenRepository : IIdentityRefreshTokenRepository
    {
        public int RevokeAllCalls { get; private set; }

        public Task<UserRefreshToken?> FindByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<UserRefreshToken?>(null);

        public Task AddAsync(UserRefreshToken refreshToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RevokeAllActiveForUserAsync(
            Guid userId,
            DateTimeOffset revokedAt,
            string? revokedByIp = null,
            CancellationToken cancellationToken = default)
        {
            RevokeAllCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRefreshTokenService : IRefreshTokenService
    {
        public string GenerateRefreshToken() => "generated-token";

        public string HashRefreshToken(string refreshToken) => $"hash:{refreshToken}";

        public bool VerifyRefreshToken(string refreshToken, string refreshTokenHash) =>
            HashRefreshToken(refreshToken) == refreshTokenHash;
    }

    private sealed class FakePasswordResetTokenRepository(
        PasswordResetToken? token,
        FakeRefreshTokenService refreshService) : IPasswordResetTokenRepository
    {
        public Task<PasswordResetToken?> FindActiveByTokenHashAsync(
            string tokenHash,
            CancellationToken cancellationToken = default)
        {
            if (token is null)
            {
                return Task.FromResult<PasswordResetToken?>(null);
            }

            return Task.FromResult<PasswordResetToken?>(
                token.TokenHash == tokenHash && token.IsActive ? token : null);
        }

        public Task AddAsync(PasswordResetToken resetToken, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task InvalidateAllActiveForUserAsync(
            Guid userId,
            DateTimeOffset invalidatedAt,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakePasswordResetSettings : IPasswordResetSettings
    {
        public int ExpirationMinutes => 60;

        public string FrontendResetUrl => "http://localhost:3000/reset-password";
    }

    private sealed class FakeAccountEmailService : IAccountEmailService
    {
        public Task SendPasswordResetEmailAsync(
            string toEmail,
            string fullName,
            string resetLink,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class FakeActivityLogService : AuditLogs.Application.Abstractions.IActivityLogService
    {
        public Task ClearPendingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task EnqueuePostCommitAsync(
            string activityType,
            string description,
            string moduleName,
            Guid? userId = null,
            string? userName = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task FlushPendingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogAuthorizationFailedAsync(
            Guid? userId,
            string? userName,
            string permission,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogErrorAsync(
            string description,
            string moduleName,
            string errorMessage,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogImmediateAsync(
            string activityType,
            string description,
            string moduleName,
            string status,
            string? errorMessage = null,
            Guid? userId = null,
            string? userName = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogLoginFailedAsync(
            string usernameOrEmail,
            string reason,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogLoginSuccessAsync(
            Guid userId,
            string userName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogLogoutAsync(
            Guid userId,
            string? userName,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task LogRefreshTokenAsync(
            Guid? userId,
            string? userName,
            string status,
            string? errorMessage = null,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
