using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using Microsoft.Extensions.Configuration;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;
using Settings.Domain.Constants;

namespace Settings.Infrastructure.Services;

public sealed class AccessPolicyService : IAccessPolicyService
{
    private const string JwtSection = "Jwt";
    private const string RefreshTokenSection = "RefreshToken";
    private const string FileStorageSection = "FileStorage";

    private readonly ISettingProvider _settingProvider;
    private readonly SettingService _settingService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly IConfiguration _configuration;
    private readonly IActivityLogService _activityLogService;

    public AccessPolicyService(
        ISettingProvider settingProvider,
        SettingService settingService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        IConfiguration configuration,
        IActivityLogService activityLogService)
    {
        _settingProvider = settingProvider;
        _settingService = settingService;
        _cacheService = cacheService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _configuration = configuration;
        _activityLogService = activityLogService;
    }

    public Task<PasswordPolicyResponse> GetPasswordPolicyAsync(CancellationToken cancellationToken = default) =>
        _cacheService.GetOrSetAsync(
            CacheKeys.AccessPolicyPassword,
            async ct =>
            {
                var minimumLength = await _settingProvider.GetIntAsync(
                    SystemSettingKeys.Password.MinimumLength,
                    8,
                    ct);

                return new PasswordPolicyResponse
                {
                    MinimumLength = minimumLength,
                    RequireUppercase = await _settingProvider.GetBoolAsync(
                        SystemSettingKeys.Password.RequireUppercase,
                        true,
                        ct),
                    RequireLowercase = await _settingProvider.GetBoolAsync(
                        SystemSettingKeys.Password.RequireLowercase,
                        true,
                        ct),
                    RequireDigit = await _settingProvider.GetBoolAsync(
                        SystemSettingKeys.Password.RequireDigit,
                        true,
                        ct),
                    RequireSpecialCharacter = await _settingProvider.GetBoolAsync(
                        SystemSettingKeys.Password.RequireSpecialCharacter,
                        false,
                        ct),
                    PasswordExpirationDays = await GetNullableIntSettingAsync(
                        SystemSettingKeys.Password.ExpirationDays,
                        ct),
                    PreventPasswordReuseCount = await _settingProvider.GetIntAsync(
                        SystemSettingKeys.Password.PreventReuseCount,
                        0,
                        ct)
                };
            },
            cancellationToken: cancellationToken);

    public async Task UpdatePasswordPolicyAsync(
        UpdatePasswordPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidatePasswordPolicy(request);

        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.MinimumLength,
            request.MinimumLength.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.RequireUppercase,
            request.RequireUppercase.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.RequireLowercase,
            request.RequireLowercase.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.RequireDigit,
            request.RequireDigit.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.RequireSpecialCharacter,
            request.RequireSpecialCharacter.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.ExpirationDays,
            request.PasswordExpirationDays?.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Password.PreventReuseCount,
            request.PreventPasswordReuseCount.ToString(),
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuditLogs.Domain.Constants.ActivityTypes.Update,
            "Updated password policy.",
            AuditLogs.Domain.Constants.AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicyPassword);
    }

    public Task<LoginPolicyResponse> GetLoginPolicyAsync(CancellationToken cancellationToken = default) =>
        _cacheService.GetOrSetAsync(
            CacheKeys.AccessPolicyLogin,
            async ct => new LoginPolicyResponse
            {
                MaxFailedLoginAttempts = await _settingProvider.GetIntAsync(
                    SystemSettingKeys.Login.MaxFailedAttempts,
                    5,
                    ct),
                LockoutDurationMinutes = await _settingProvider.GetIntAsync(
                    SystemSettingKeys.Login.LockoutDurationMinutes,
                    15,
                    ct),
                EnableLockout = await _settingProvider.GetBoolAsync(
                    SystemSettingKeys.Login.EnableLockout,
                    true,
                    ct),
                RequireConfirmedEmail = await _settingProvider.GetBoolAsync(
                    SystemSettingKeys.Login.RequireConfirmedEmail,
                    false,
                    ct)
            },
            cancellationToken: cancellationToken);

    public async Task UpdateLoginPolicyAsync(
        UpdateLoginPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateLoginPolicy(request);

        await UpdatePolicySettingAsync(
            SystemSettingKeys.Login.MaxFailedAttempts,
            request.MaxFailedLoginAttempts.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Login.LockoutDurationMinutes,
            request.LockoutDurationMinutes.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Login.EnableLockout,
            request.EnableLockout.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Login.RequireConfirmedEmail,
            request.RequireConfirmedEmail.ToString(),
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuditLogs.Domain.Constants.ActivityTypes.Update,
            "Updated login policy.",
            AuditLogs.Domain.Constants.AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicyLogin);
    }

    public Task<SessionPolicyResponse> GetSessionPolicyAsync(CancellationToken cancellationToken = default) =>
        _cacheService.GetOrSetAsync(
            CacheKeys.AccessPolicySession,
            async ct =>
            {
                var configAccessMinutes = _configuration.GetValue($"{JwtSection}:AccessTokenExpirationMinutes", 30);
                var configRefreshDays = _configuration.GetValue($"{RefreshTokenSection}:ExpirationDays", 7);

                return new SessionPolicyResponse
                {
                    AccessTokenExpirationMinutes = await _settingProvider.GetIntAsync(
                        SystemSettingKeys.Session.AccessTokenExpirationMinutes,
                        configAccessMinutes,
                        ct),
                    RefreshTokenExpirationDays = await _settingProvider.GetIntAsync(
                        SystemSettingKeys.Session.RefreshTokenExpirationDays,
                        configRefreshDays,
                        ct),
                    SessionTimeoutMinutes = await GetNullableIntSettingAsync(
                        SystemSettingKeys.Session.SessionTimeoutMinutes,
                        ct),
                    RefreshTokenReuseDetectionEnabled = await _settingProvider.GetBoolAsync(
                        SystemSettingKeys.Session.RefreshTokenReuseDetectionEnabled,
                        true,
                        ct)
                };
            },
            cancellationToken: cancellationToken);

    public async Task UpdateSessionPolicyAsync(
        UpdateSessionPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateSessionPolicy(request);

        await UpdatePolicySettingAsync(
            SystemSettingKeys.Session.AccessTokenExpirationMinutes,
            request.AccessTokenExpirationMinutes.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Session.RefreshTokenExpirationDays,
            request.RefreshTokenExpirationDays.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Session.SessionTimeoutMinutes,
            request.SessionTimeoutMinutes?.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Session.RefreshTokenReuseDetectionEnabled,
            request.RefreshTokenReuseDetectionEnabled.ToString(),
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuditLogs.Domain.Constants.ActivityTypes.Update,
            "Updated session policy.",
            AuditLogs.Domain.Constants.AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicySession);
    }

    public Task<MaintenancePolicyResponse> GetMaintenancePolicyAsync(CancellationToken cancellationToken = default) =>
        _cacheService.GetOrSetAsync(
            CacheKeys.MaintenancePolicy,
            async ct => new MaintenancePolicyResponse
            {
                Enabled = await _settingProvider.GetBoolAsync(
                    SystemSettingKeys.Maintenance.Enabled,
                    false,
                    ct),
                Message = await _settingProvider.GetStringAsync(
                    SystemSettingKeys.Maintenance.Message,
                    cancellationToken: ct),
                StartAt = SettingValueConverter.ParseDateTimeOffset(
                    await _settingProvider.GetStringAsync(
                        SystemSettingKeys.Maintenance.StartAt,
                        cancellationToken: ct)),
                EndAt = SettingValueConverter.ParseDateTimeOffset(
                    await _settingProvider.GetStringAsync(
                        SystemSettingKeys.Maintenance.EndAt,
                        cancellationToken: ct)),
                AllowAdminBypass = await _settingProvider.GetBoolAsync(
                    SystemSettingKeys.Maintenance.AllowAdminBypass,
                    true,
                    ct)
            },
            cancellationToken: cancellationToken);

    public async Task UpdateMaintenancePolicyAsync(
        UpdateMaintenancePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateMaintenancePolicy(request);

        await UpdatePolicySettingAsync(
            SystemSettingKeys.Maintenance.Enabled,
            request.Enabled.ToString(),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Maintenance.Message,
            request.Message,
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Maintenance.StartAt,
            request.StartAt?.ToString("O"),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Maintenance.EndAt,
            request.EndAt?.ToString("O"),
            cancellationToken);
        await UpdatePolicySettingAsync(
            SystemSettingKeys.Maintenance.AllowAdminBypass,
            request.AllowAdminBypass.ToString(),
            cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            AuditLogs.Domain.Constants.ActivityTypes.Update,
            "Updated maintenance policy.",
            AuditLogs.Domain.Constants.AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.MaintenancePolicy);
    }

    private async Task UpdatePolicySettingAsync(
        string key,
        string? value,
        CancellationToken cancellationToken)
    {
        await _settingService.UpdateSettingValueByKeyAsync(key, value, cancellationToken);
    }

    private async Task<int?> GetNullableIntSettingAsync(string key, CancellationToken cancellationToken)
    {
        var raw = await _settingProvider.GetStringAsync(key, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw, out var parsed) ? parsed : null;
    }

    internal static void ValidatePasswordPolicy(UpdatePasswordPolicyRequest request)
    {
        if (request.MinimumLength is < 8 or > 128)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidPasswordPolicy,
                "Minimum length must be between 8 and 128.");
        }

        if (request.PasswordExpirationDays is not null and (< 1 or > 365))
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidPasswordPolicy,
                "Password expiration days must be between 1 and 365 when provided.");
        }

        if (request.PreventPasswordReuseCount is < 0 or > 24)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidPasswordPolicy,
                "Prevent password reuse count must be between 0 and 24.");
        }

        if (request.MinimumLength < 12 &&
            !request.RequireUppercase &&
            !request.RequireLowercase &&
            !request.RequireDigit &&
            !request.RequireSpecialCharacter)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidPasswordPolicy,
                "At least one complexity rule must be enabled when minimum length is below 12.");
        }
    }

    internal static void ValidateLoginPolicy(UpdateLoginPolicyRequest request)
    {
        if (request.MaxFailedLoginAttempts is < 1 or > 20)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidLoginPolicy,
                "Max failed login attempts must be between 1 and 20.");
        }

        if (request.LockoutDurationMinutes is < 1 or > 1440)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidLoginPolicy,
                "Lockout duration minutes must be between 1 and 1440.");
        }
    }

    internal static void ValidateSessionPolicy(UpdateSessionPolicyRequest request)
    {
        if (request.AccessTokenExpirationMinutes is < 1 or > 1440)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidSessionPolicy,
                "Access token expiration minutes must be between 1 and 1440.");
        }

        if (request.RefreshTokenExpirationDays is < 1 or > 365)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidSessionPolicy,
                "Refresh token expiration days must be between 1 and 365.");
        }

        if (request.SessionTimeoutMinutes is not null and (< 5 or > 1440))
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidSessionPolicy,
                "Session timeout minutes must be between 5 and 1440 when provided.");
        }
    }

    internal static void ValidateMaintenancePolicy(UpdateMaintenancePolicyRequest request)
    {
        if (request.StartAt.HasValue &&
            request.EndAt.HasValue &&
            request.StartAt > request.EndAt)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidMaintenancePolicy,
                "Maintenance start time must be before end time.");
        }

        if (request.Message?.Length > 1000)
        {
            throw new BadRequestException(
                AccessPolicyErrors.InvalidMaintenancePolicy,
                "Maintenance message cannot exceed 1000 characters.");
        }
    }
}
