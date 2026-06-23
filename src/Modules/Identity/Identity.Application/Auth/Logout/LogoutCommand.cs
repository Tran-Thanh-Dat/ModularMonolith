using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using Identity.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Auth.Logout;
public sealed record LogoutCommand(
    string RefreshToken,
    string? IpAddress = null) : ICommand;

public sealed class LogoutCommandValidator : FluentValidation.AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(command => command.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh token is required.");
    }
}

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRefreshTokenRepository _refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IIdentityUserRepository userRepository,
        IIdentityRefreshTokenRepository refreshTokenRepository,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<LogoutCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _refreshTokenService = refreshTokenService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.Revoke(_dateTimeProvider.UtcNow, request.IpAddress);

            var user = await _userRepository.FindActiveByIdWithRolesAsync(storedToken.UserId, cancellationToken);

            _logger.LogInformation(
                "Logout completed for {UserId}",
                storedToken.UserId);

            await _activityLogService.LogLogoutAsync(
                storedToken.UserId,
                user?.UserName,
                cancellationToken);
        }

        return Result.Success();
    }
}
