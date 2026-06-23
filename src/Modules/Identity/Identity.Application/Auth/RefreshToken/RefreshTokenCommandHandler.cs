using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Identity.Application.Abstractions;
using Identity.Domain.RefreshTokens;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Settings.Application.Abstractions;

namespace Identity.Application.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRefreshTokenSettings _refreshTokenSettings;
    private readonly IAccessPolicyService _accessPolicyService;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheOperationBuffer _cacheOperationBuffer;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IIdentityUserRepository userRepository,
        IIdentityRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenSettings refreshTokenSettings,
        IAccessPolicyService accessPolicyService,
        IActivityLogService activityLogService,
        ICacheOperationBuffer cacheOperationBuffer,
        IOptions<CacheOptions> cacheOptions,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _dateTimeProvider = dateTimeProvider;
        _refreshTokenSettings = refreshTokenSettings;
        _accessPolicyService = accessPolicyService;
        _activityLogService = activityLogService;
        _cacheOperationBuffer = cacheOperationBuffer;
        _cacheOptions = cacheOptions.Value;
        _logger = logger;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var tokenHash = _refreshTokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await _refreshTokenRepository.FindByTokenHashAsync(tokenHash, cancellationToken);

        if (storedToken is null)
        {
            await _activityLogService.LogRefreshTokenAsync(
                null,
                null,
                AuditLogStatus.Failed,
                "Invalid or expired refresh token.",
                cancellationToken);

            return Result<RefreshTokenResponse>.Failure(
                AuthErrors.RefreshTokenInvalid,
                "Invalid refresh token.");
        }

        if (storedToken.IsRevoked)
        {
            var reusePolicy = await _accessPolicyService.GetSessionPolicyAsync(cancellationToken);
            if (reusePolicy.RefreshTokenReuseDetectionEnabled)
            {
                await HandleRefreshTokenReuseAsync(storedToken, request.IpAddress, cancellationToken);
            }

            return Result<RefreshTokenResponse>.Failure(
                AuthErrors.RefreshTokenInvalid,
                "Invalid refresh token.");
        }

        if (storedToken.IsExpired)
        {
            await _activityLogService.LogRefreshTokenAsync(
                storedToken.UserId,
                null,
                AuditLogStatus.Failed,
                "Invalid or expired refresh token.",
                cancellationToken);

            return Result<RefreshTokenResponse>.Failure(
                AuthErrors.RefreshTokenInvalid,
                "Invalid refresh token.");
        }

        var user = await _userRepository.FindActiveByIdWithRolesAsync(storedToken.UserId, cancellationToken);

        if (user is null)
        {
            await _activityLogService.LogRefreshTokenAsync(
                storedToken.UserId,
                null,
                AuditLogStatus.Failed,
                "User not found or inactive.",
                cancellationToken);

            return Result<RefreshTokenResponse>.Failure(
                AuthErrors.RefreshTokenInvalid,
                "Invalid refresh token.");
        }
        storedToken.Revoke(_dateTimeProvider.UtcNow, request.IpAddress);

        var roles = user.Roles.Where(r => r.IsActive).Select(r => r.Code).Distinct().ToArray();
        var permissions = user.Roles
            .Where(r => r.IsActive)
            .SelectMany(r => r.Permissions)
            .Select(p => p.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _cacheOperationBuffer.EnqueueSet(
            CacheKeys.UserPermissions(user.Id),
            permissions,
            TimeSpan.FromMinutes(_cacheOptions.UserPermissionsExpirationMinutes));

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
            user,
            roles,
            permissions,
            cancellationToken);

        var sessionPolicy = await _accessPolicyService.GetSessionPolicyAsync(cancellationToken);
        var refreshExpirationDays = sessionPolicy.RefreshTokenExpirationDays > 0
            ? sessionPolicy.RefreshTokenExpirationDays
            : _refreshTokenSettings.ExpirationDays;

        var rawRefreshToken = _refreshTokenService.GenerateRefreshToken();
        var newTokenHash = _refreshTokenService.HashRefreshToken(rawRefreshToken);
        var refreshExpiresAt = _dateTimeProvider.UtcNow.AddDays(refreshExpirationDays);

        var newRefreshToken = UserRefreshToken.Create(
            user.Id,
            newTokenHash,
            refreshExpiresAt,
            _dateTimeProvider.UtcNow,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        _logger.LogInformation(
            "Refresh token rotated for {UserId}",
            user.Id);

        await _activityLogService.LogRefreshTokenAsync(
            user.Id,
            user.UserName,
            AuditLogStatus.Success,
            cancellationToken: cancellationToken);

        return Result<RefreshTokenResponse>.Success(new RefreshTokenResponse
        {
            AccessToken = accessToken.AccessToken,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        });
    }

    private async Task HandleRefreshTokenReuseAsync(
        UserRefreshToken storedToken,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        await _refreshTokenRepository.RevokeAllActiveForUserAsync(
            storedToken.UserId,
            _dateTimeProvider.UtcNow,
            ipAddress,
            cancellationToken);

        _logger.LogWarning(
            "Refresh token reuse detected for user {UserId}",
            storedToken.UserId);

        await _activityLogService.LogImmediateAsync(
            ActivityTypes.RefreshTokenReuseDetected,
            "Possible refresh token reuse detected.",
            AuditLogConstants.Modules.Identity,
            AuditLogStatus.Failed,
            errorMessage: AuthErrors.RefreshTokenReuseDetected,
            userId: storedToken.UserId,
            cancellationToken: cancellationToken);
    }
}
