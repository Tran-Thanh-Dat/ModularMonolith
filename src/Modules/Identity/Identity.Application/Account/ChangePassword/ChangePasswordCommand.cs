using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Application.Validation;
using MediatR;
using Microsoft.Extensions.Logging;
using Settings.Application.Abstractions;

namespace Identity.Application.Account.ChangePassword;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string? IpAddress) : ICommand;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator(IPasswordPolicyValidator passwordPolicyValidator)
    {
        RuleFor(command => command.CurrentPassword)
            .NotEmpty();

        RuleFor(command => command.NewPassword)
            .ApplyPasswordPolicy(passwordPolicyValidator)
            .NotEqual(command => command.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }
}

public sealed class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IIdentityRefreshTokenRepository _refreshTokenRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<ChangePasswordCommandHandler> _logger;

    public ChangePasswordCommandHandler(
        ICurrentUserService currentUserService,
        IIdentityUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IIdentityRefreshTokenRepository refreshTokenRepository,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<ChangePasswordCommandHandler> logger)
    {
        _currentUserService = currentUserService;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _refreshTokenRepository = refreshTokenRepository;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        var userId = _currentUserService.UserId.Value;
        var user = await _userRepository.FindActiveByIdForUpdateAsync(userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedException(CommonErrors.Unauthorized, "User is not authenticated.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            _logger.LogWarning("Change password failed for user {UserId}: invalid current password.", userId);

            return Result.Failure(
                AccountErrors.CurrentPasswordInvalid,
                "Current password is incorrect.");
        }

        user.ChangePassword(_passwordHasher.HashPassword(request.NewPassword));

        await _refreshTokenRepository.RevokeAllActiveForUserAsync(
            userId,
            _dateTimeProvider.UtcNow,
            request.IpAddress,
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.ChangePassword,
            $"User '{user.UserName}' changed their password.",
            AuditLogConstants.Modules.Identity,
            userId,
            user.UserName,
            cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}.", userId);

        return Result.Success();
    }
}
