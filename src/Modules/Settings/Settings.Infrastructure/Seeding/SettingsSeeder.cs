using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Settings.Application.Abstractions;
using Settings.Domain.Constants;
using Settings.Domain.Settings;
using Settings.Infrastructure.Persistence;

namespace Settings.Infrastructure.Seeding;

public sealed class SettingsSeeder : ISettingsSeeder
{
    private const string JwtSection = "Jwt";
    private const string RefreshTokenSection = "RefreshToken";
    private const string FileStorageSection = "FileStorage";

    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SettingsSeeder> _logger;

    public SettingsSeeder(
        SettingsUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider,
        IConfiguration configuration,
        ILogger<SettingsSeeder> logger)
    {
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var accessTokenMinutes = _configuration.GetValue($"{JwtSection}:AccessTokenExpirationMinutes", 30);
        var refreshTokenDays = _configuration.GetValue($"{RefreshTokenSection}:ExpirationDays", 7);
        var maxFileSizeMb = _configuration.GetValue($"{FileStorageSection}:MaxFileSizeMb", 20);

        var definitions = BuildDefaultDefinitions(accessTokenMinutes, refreshTokenDays, maxFileSizeMb);
        var createdCount = 0;

        foreach (var definition in definitions)
        {
            if (await SettingExistsAsync(definition.Key, cancellationToken))
            {
                continue;
            }

            var setting = SystemSetting.Create(
                definition.Key,
                definition.Group,
                definition.Name,
                definition.Description,
                definition.Value,
                definition.DefaultValue,
                definition.DataType,
                definition.IsEncrypted,
                definition.IsSensitive,
                isSystem: true,
                isEditable: true,
                definition.SortOrder,
                _dateTimeProvider.UtcNow);

            await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting, cancellationToken);
            createdCount++;
        }

        if (createdCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Seeded {Count} default system settings.", createdCount);
        }
    }

    private async Task<bool> SettingExistsAsync(string key, CancellationToken cancellationToken) =>
        await _unitOfWork.Repository<SystemSetting, Guid>()
            .QueryReadOnly()
            .AnyAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

    private static IReadOnlyCollection<SettingSeedDefinition> BuildDefaultDefinitions(
        int accessTokenMinutes,
        int refreshTokenDays,
        int maxFileSizeMb) =>
    [
        Def(SystemSettingKeys.Password.MinimumLength, SettingGroups.PasswordPolicy, "Minimum Password Length", SettingDataTypes.Number, "8", 1),
        Def(SystemSettingKeys.Password.RequireUppercase, SettingGroups.PasswordPolicy, "Require Uppercase", SettingDataTypes.Boolean, "True", 2),
        Def(SystemSettingKeys.Password.RequireLowercase, SettingGroups.PasswordPolicy, "Require Lowercase", SettingDataTypes.Boolean, "True", 3),
        Def(SystemSettingKeys.Password.RequireDigit, SettingGroups.PasswordPolicy, "Require Digit", SettingDataTypes.Boolean, "True", 4),
        Def(SystemSettingKeys.Password.RequireSpecialCharacter, SettingGroups.PasswordPolicy, "Require Special Character", SettingDataTypes.Boolean, "False", 5),
        Def(SystemSettingKeys.Password.ExpirationDays, SettingGroups.PasswordPolicy, "Password Expiration Days", SettingDataTypes.Number, null, 6),
        Def(SystemSettingKeys.Password.PreventReuseCount, SettingGroups.PasswordPolicy, "Prevent Password Reuse Count", SettingDataTypes.Number, "0", 7),

        Def(SystemSettingKeys.Login.MaxFailedAttempts, SettingGroups.LoginPolicy, "Max Failed Login Attempts", SettingDataTypes.Number, "5", 1),
        Def(SystemSettingKeys.Login.LockoutDurationMinutes, SettingGroups.LoginPolicy, "Lockout Duration Minutes", SettingDataTypes.Number, "15", 2),
        Def(SystemSettingKeys.Login.EnableLockout, SettingGroups.LoginPolicy, "Enable Lockout", SettingDataTypes.Boolean, "True", 3),
        Def(SystemSettingKeys.Login.RequireConfirmedEmail, SettingGroups.LoginPolicy, "Require Confirmed Email", SettingDataTypes.Boolean, "False", 4),

        Def(SystemSettingKeys.Session.AccessTokenExpirationMinutes, SettingGroups.SessionPolicy, "Access Token Expiration Minutes", SettingDataTypes.Number, accessTokenMinutes.ToString(), 1),
        Def(SystemSettingKeys.Session.RefreshTokenExpirationDays, SettingGroups.SessionPolicy, "Refresh Token Expiration Days", SettingDataTypes.Number, refreshTokenDays.ToString(), 2),
        Def(SystemSettingKeys.Session.SessionTimeoutMinutes, SettingGroups.SessionPolicy, "Session Timeout Minutes", SettingDataTypes.Number, null, 3),
        Def(SystemSettingKeys.Session.RefreshTokenReuseDetectionEnabled, SettingGroups.SessionPolicy, "Refresh Token Reuse Detection Enabled", SettingDataTypes.Boolean, "True", 4),

        Def(SystemSettingKeys.FileStorage.MaxFileSizeMb, SettingGroups.FileStorage, "Max File Size (MB)", SettingDataTypes.Number, maxFileSizeMb.ToString(), 1),

        Def(SystemSettingKeys.Notification.EmailEnabled, SettingGroups.Notification, "Email Notifications Enabled", SettingDataTypes.Boolean, "True", 1),
        Def(SystemSettingKeys.Notification.InAppEnabled, SettingGroups.Notification, "In-App Notifications Enabled", SettingDataTypes.Boolean, "True", 2),

        Def(SystemSettingKeys.Maintenance.Enabled, SettingGroups.Maintenance, "Maintenance Enabled", SettingDataTypes.Boolean, "False", 1),
        Def(SystemSettingKeys.Maintenance.Message, SettingGroups.Maintenance, "Maintenance Message", SettingDataTypes.String, null, 2),
        Def(SystemSettingKeys.Maintenance.StartAt, SettingGroups.Maintenance, "Maintenance Start At", SettingDataTypes.String, null, 3),
        Def(SystemSettingKeys.Maintenance.EndAt, SettingGroups.Maintenance, "Maintenance End At", SettingDataTypes.String, null, 4),
        Def(SystemSettingKeys.Maintenance.AllowAdminBypass, SettingGroups.Maintenance, "Allow Admin Bypass", SettingDataTypes.Boolean, "True", 5)
    ];

    private static SettingSeedDefinition Def(
        string key,
        string group,
        string name,
        string dataType,
        string? value,
        int sortOrder) =>
        new(key, group, name, null, value, value, dataType, false, false, sortOrder);

    private sealed record SettingSeedDefinition(
        string Key,
        string Group,
        string Name,
        string? Description,
        string? Value,
        string? DefaultValue,
        string DataType,
        bool IsEncrypted,
        bool IsSensitive,
        int SortOrder);
}
