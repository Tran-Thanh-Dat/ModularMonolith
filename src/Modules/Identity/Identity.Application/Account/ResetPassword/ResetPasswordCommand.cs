using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Validation;
using MediatR;
using Microsoft.Extensions.Logging;
using Settings.Application.Abstractions;

namespace Identity.Application.Account.ResetPassword;

public sealed record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string? IpAddress) : ICommand;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator(IPasswordPolicyValidator passwordPolicyValidator)
    {
        RuleFor(command => command.Token)
            .NotEmpty();

        RuleFor(command => command.NewPassword)
            .ApplyPasswordPolicy(passwordPolicyValidator);
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityRefreshTokenRepository _refreshTokenRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IRefreshTokenService refreshTokenService,
        IPasswordHasher passwordHasher,
        IIdentityRefreshTokenRepository refreshTokenRepository,
        IIdentityUserRepository userRepository,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _refreshTokenService = refreshTokenService;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashRefreshToken(request.Token);
        var resetToken = await _passwordResetTokenRepository.FindActiveByTokenHashAsync(tokenHash, cancellationToken);

        if (resetToken is null || !resetToken.IsActive)
        {
            _logger.LogWarning("Password reset failed: invalid or expired token.");

            return Result.Failure(
                AccountErrors.PasswordResetTokenInvalid,
                "Password reset token is invalid or has expired.");
        }

        var user = await _userRepository.FindActiveByIdForUpdateAsync(resetToken.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure(
                AccountErrors.PasswordResetTokenInvalid,
                "Password reset token is invalid or has expired.");
        }

        var now = _dateTimeProvider.UtcNow;

        user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));
        resetToken.MarkUsed(now);

        await _refreshTokenRepository.RevokeAllActiveForUserAsync(
            user.Id,
            now,
            request.IpAddress,
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.ChangePassword,
            $"User '{user.UserName}' reset their password via email token.",
            AuditLogConstants.Modules.Identity,
            user.Id,
            user.UserName,
            cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}.", user.Id);

        return Result.Success();
    }
}
