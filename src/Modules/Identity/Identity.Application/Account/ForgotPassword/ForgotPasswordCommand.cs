using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Identity.Application.Abstractions;
using Identity.Domain.PasswordResetTokens;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Account.ForgotPassword;

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } =
        "If an account exists with that email, a password reset link has been sent.";
}

public sealed record ForgotPasswordCommand(
    string Email,
    string? IpAddress) : ICommand<ForgotPasswordResponse>;

public sealed class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);
    }
}

public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, Result<ForgotPasswordResponse>>
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPasswordResetSettings _passwordResetSettings;
    private readonly IAccountEmailService _accountEmailService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IIdentityUserRepository userRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IRefreshTokenService refreshTokenService,
        IPasswordResetSettings passwordResetSettings,
        IAccountEmailService accountEmailService,
        IDateTimeProvider dateTimeProvider,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _refreshTokenService = refreshTokenService;
        _passwordResetSettings = passwordResetSettings;
        _accountEmailService = accountEmailService;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<Result<ForgotPasswordResponse>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.FindActiveByEmailAsync(normalizedEmail, cancellationToken);

        if (user is not null)
        {
            var now = _dateTimeProvider.UtcNow;

            await _passwordResetTokenRepository.InvalidateAllActiveForUserAsync(user.Id, now, cancellationToken);

            var rawToken = _refreshTokenService.GenerateRefreshToken();
            var tokenHash = _refreshTokenService.HashRefreshToken(rawToken);
            var expiresAt = now.AddMinutes(_passwordResetSettings.ExpirationMinutes);

            var resetToken = PasswordResetToken.Create(
                user.Id,
                tokenHash,
                expiresAt,
                now,
                request.IpAddress);

            await _passwordResetTokenRepository.AddAsync(resetToken, cancellationToken);

            var resetLink = BuildResetLink(_passwordResetSettings.FrontendResetUrl, rawToken);

            try
            {
                await _accountEmailService.SendPasswordResetEmailAsync(
                    user.Email,
                    user.FullName,
                    resetLink,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to send password reset email to user {UserId}.",
                    user.Id);
            }

            _logger.LogInformation("Password reset requested for user {UserId}.", user.Id);
        }
        else
        {
            _logger.LogDebug("Password reset requested for unknown email {Email}.", normalizedEmail);
        }

        return Result<ForgotPasswordResponse>.Success(new ForgotPasswordResponse());
    }

    private static string BuildResetLink(string frontendResetUrl, string rawToken)
    {
        var separator = frontendResetUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{frontendResetUrl.TrimEnd('/')}{separator}token={Uri.EscapeDataString(rawToken)}";
    }
}
