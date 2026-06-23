using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Results;
using Identity.Application.Abstractions;
using Identity.Domain.RefreshTokens;
using Identity.Domain.Roles;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Settings.Application.Abstractions;

namespace Identity.Application.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    private readonly IIdentityUserRepository _userRepository;
    private readonly IIdentityRefreshTokenRepository _refreshTokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IRefreshTokenSettings _refreshTokenSettings;
    private readonly IAccessPolicyService _accessPolicyService;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheOperationBuffer _cacheOperationBuffer;
    private readonly CacheOptions _cacheOptions;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IIdentityUserRepository userRepository,
        IIdentityRefreshTokenRepository refreshTokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IDateTimeProvider dateTimeProvider,
        IRefreshTokenSettings refreshTokenSettings,
        IAccessPolicyService accessPolicyService,
        IActivityLogService activityLogService,
        ICacheOperationBuffer cacheOperationBuffer,
        IOptions<CacheOptions> cacheOptions,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _passwordHasher = passwordHasher;
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

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalized = request.UserNameOrEmail.Trim().ToLowerInvariant();

        var user = await _userRepository.FindActiveByUserNameOrEmailAsync(normalized, cancellationToken);

        if (user is null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning(
                "Login failed for {UserNameOrEmail} from {IpAddress}",
                request.UserNameOrEmail,
                request.IpAddress);

            await _activityLogService.LogLoginFailedAsync(
                request.UserNameOrEmail,
                "Invalid username/email or password.",
                cancellationToken);

            return Result<LoginResponse>.Failure(
                AuthErrors.InvalidCredentials,
                "Invalid username/email or password.");
        }
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
        var refreshTokenHash = _refreshTokenService.HashRefreshToken(rawRefreshToken);
        var refreshExpiresAt = _dateTimeProvider.UtcNow.AddDays(refreshExpirationDays);

        var refreshTokenEntity = UserRefreshToken.Create(
            user.Id,
            refreshTokenHash,
            refreshExpiresAt,
            _dateTimeProvider.UtcNow,
            request.IpAddress);

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        user.MarkLoggedIn(_dateTimeProvider.UtcNow);

        _logger.LogInformation(
            "Login succeeded for {UserId} {UserName} from {IpAddress}",
            user.Id,
            user.UserName,
            request.IpAddress);

        await _activityLogService.LogLoginSuccessAsync(user.Id, user.UserName, cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse
        {
            AccessToken = accessToken.AccessToken,
            AccessTokenExpiresAt = accessToken.ExpiresAt,
            RefreshToken = rawRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt,
            User = new LoginUserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles,
                Permissions = permissions
            }
        });
    }
}
