using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Settings.Application.Settings;
using Settings.Domain.Constants;
using Settings.Domain.Settings;
using Settings.Infrastructure.Persistence;
using Settings.Infrastructure.Seeding;
using Settings.Infrastructure.Services;
using Xunit;

namespace Settings.Infrastructure.Tests.Services;

public sealed class SettingServiceTests : IDisposable
{
    private readonly SettingsDbContext _dbContext;
    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly RecordingCacheInvalidationBuffer _cacheInvalidation;
    private readonly RecordingCacheService _cacheService;
    private readonly SettingService _service;

    public SettingServiceTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new SettingsDbContext(options, currentUser, dateTime);
        _unitOfWork = new SettingsUnitOfWork(_dbContext, NullLogger<SettingsUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();
        _cacheInvalidation = new RecordingCacheInvalidationBuffer();
        _cacheService = new RecordingCacheService();

        _service = new SettingService(
            _unitOfWork,
            currentUser,
            dateTime,
            _activityLog,
            _cacheService,
            _cacheInvalidation,
            NullLogger<SettingService>.Instance);
    }

    [Fact]
    public async Task CreateSetting_WhenKeyIsUnique_ReturnsIdAndEnqueuesActivityAndCacheInvalidation()
    {
        var id = await _service.CreateAsync(
            new CreateSettingRequest
            {
                Key = "Custom.Setting",
                Group = SettingGroups.Business,
                Name = "Custom Setting",
                DataType = SettingDataTypes.String,
                Value = "value"
            });

        await _unitOfWork.SaveChangesAsync();

        Assert.NotEqual(Guid.Empty, id);
        Assert.Single(_activityLog.PostCommitEntries);
        Assert.Contains(CacheKeys.SettingListPrefix, _cacheInvalidation.RemovedPrefixes);
    }

    [Fact]
    public async Task CreateSetting_WhenKeyExists_ReturnsKeyAlreadyExists()
    {
        await _service.CreateAsync(
            new CreateSettingRequest
            {
                Key = "Duplicate.Key",
                Group = SettingGroups.Business,
                Name = "First",
                DataType = SettingDataTypes.String
            });
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync(
                new CreateSettingRequest
                {
                    Key = "Duplicate.Key",
                    Group = SettingGroups.Business,
                    Name = "Second",
                    DataType = SettingDataTypes.String
                }));

        Assert.Equal(SettingErrors.KeyAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task UpdateValue_WhenValid_UpdatesSettingAndInvalidatesCache()
    {
        var id = await _service.CreateAsync(
            new CreateSettingRequest
            {
                Key = "Custom.Value",
                Group = SettingGroups.Business,
                Name = "Value Setting",
                DataType = SettingDataTypes.String,
                Value = "old"
            });
        await _unitOfWork.SaveChangesAsync();
        _cacheInvalidation.RemovedKeys.Clear();

        await _service.UpdateValueAsync(id, "new");
        await _unitOfWork.SaveChangesAsync();

        var updated = await _dbContext.SystemSettings.SingleAsync(s => s.Id == id);
        Assert.Equal("new", updated.Value);
        Assert.Contains(CacheKeys.SettingDetail(id), _cacheInvalidation.RemovedKeys);
    }

    [Fact]
    public async Task UpdateValue_WhenNotEditable_ReturnsNotEditable()
    {
        var setting = SystemSetting.Create(
            "Locked.Setting",
            SettingGroups.Business,
            "Locked",
            null,
            "value",
            "value",
            SettingDataTypes.String,
            false,
            false,
            true,
            isEditable: false,
            0,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateValueAsync(setting.Id, "changed"));

        Assert.Equal(SettingErrors.NotEditable, exception.Code);
    }

    [Fact]
    public async Task DeleteSetting_WhenSystemSetting_IsBlocked()
    {
        var setting = SystemSetting.Create(
            SystemSettingKeys.Maintenance.Enabled,
            SettingGroups.Maintenance,
            "Maintenance Enabled",
            null,
            "False",
            "False",
            SettingDataTypes.Boolean,
            false,
            false,
            isSystem: true,
            true,
            1,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.DeleteAsync(setting.Id));

        Assert.Equal(SettingErrors.SystemSettingCannotBeDeleted, exception.Code);
    }

    [Fact]
    public async Task GetByIdAsync_MasksSensitiveValueWithoutPermission()
    {
        var setting = SystemSetting.Create(
            "Secret.Key",
            SettingGroups.Security,
            "Secret",
            null,
            "secret-value",
            "secret-value",
            SettingDataTypes.String,
            false,
            isSensitive: true,
            false,
            true,
            0,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var detail = await _service.GetByIdAsync(setting.Id, includeSensitive: false);

        Assert.NotNull(detail);
        Assert.True(detail!.IsValueMasked);
        Assert.Equal("***", detail.Value);
    }

    [Fact]
    public async Task UpdateValue_WithInvalidDataType_ReturnsInvalidValue()
    {
        var id = await _service.CreateAsync(
            new CreateSettingRequest
            {
                Key = "Number.Setting",
                Group = SettingGroups.Business,
                Name = "Number",
                DataType = SettingDataTypes.Number,
                Value = "10"
            });
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.UpdateValueAsync(id, "not-a-number"));

        Assert.Equal(SettingErrors.InvalidValue, exception.Code);
    }

    [Fact]
    public async Task GetByIdAsync_UsesCacheOnSecondRead()
    {
        var id = await _service.CreateAsync(
            new CreateSettingRequest
            {
                Key = "Cached.Setting",
                Group = SettingGroups.Business,
                Name = "Cached",
                DataType = SettingDataTypes.String,
                Value = "value"
            });
        await _unitOfWork.SaveChangesAsync();

        await _service.GetByIdAsync(id, includeSensitive: false);
        var setCountAfterFirst = _cacheService.SetOperations.Count;

        await _service.GetByIdAsync(id, includeSensitive: false);

        Assert.Equal(setCountAfterFirst, _cacheService.SetOperations.Count);
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class SettingProviderTests : IDisposable
{
    private readonly SettingsDbContext _dbContext;
    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly RecordingCacheService _cacheService;
    private readonly SettingProvider _provider;

    public SettingProviderTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new SettingsDbContext(options, currentUser, dateTime);
        _unitOfWork = new SettingsUnitOfWork(_dbContext, NullLogger<SettingsUnitOfWork>.Instance);
        _cacheService = new RecordingCacheService();
        _provider = new SettingProvider(_unitOfWork, _cacheService);
    }

    [Fact]
    public async Task GetIntAsync_ConvertsStoredValue()
    {
        var setting = SystemSetting.Create(
            "Count.Setting",
            SettingGroups.Business,
            "Count",
            null,
            "42",
            "42",
            SettingDataTypes.Number,
            false,
            false,
            false,
            true,
            0,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var value = await _provider.GetIntAsync("Count.Setting", 0);

        Assert.Equal(42, value);
    }

    [Fact]
    public async Task GetBoolAsync_ConvertsStoredValue()
    {
        var setting = SystemSetting.Create(
            "Flag.Setting",
            SettingGroups.Business,
            "Flag",
            null,
            "True",
            "True",
            SettingDataTypes.Boolean,
            false,
            false,
            false,
            true,
            0,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        var value = await _provider.GetBoolAsync("Flag.Setting", false);

        Assert.True(value);
    }

    [Fact]
    public async Task GetStringAsync_UsesCacheOnSecondRead()
    {
        var setting = SystemSetting.Create(
            "Provider.Cache",
            SettingGroups.Business,
            "Cache",
            null,
            "cached",
            "cached",
            SettingDataTypes.String,
            false,
            false,
            false,
            true,
            0,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();

        await _provider.GetStringAsync("Provider.Cache");
        var setCountAfterFirst = _cacheService.SetOperations.Count;

        await _provider.GetStringAsync("Provider.Cache");

        Assert.Equal(setCountAfterFirst, _cacheService.SetOperations.Count);
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class AccessPolicyServiceTests : IDisposable
{
    private readonly SettingsDbContext _dbContext;
    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly SettingService _settingService;
    private readonly AccessPolicyService _accessPolicyService;
    private readonly RecordingCacheService _cacheService;
    private readonly RecordingCacheInvalidationBuffer _cacheInvalidation;

    public AccessPolicyServiceTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new SettingsDbContext(options, currentUser, dateTime);
        _unitOfWork = new SettingsUnitOfWork(_dbContext, NullLogger<SettingsUnitOfWork>.Instance);
        _cacheService = new RecordingCacheService();
        _cacheInvalidation = new RecordingCacheInvalidationBuffer();
        var activityLog = new FakeActivityLogService();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:AccessTokenExpirationMinutes"] = "45",
                ["RefreshToken:ExpirationDays"] = "10"
            })
            .Build();

        _settingService = new SettingService(
            _unitOfWork,
            currentUser,
            dateTime,
            activityLog,
            _cacheService,
            _cacheInvalidation,
            NullLogger<SettingService>.Instance);

        var provider = new SettingProvider(_unitOfWork, _cacheService);
        _accessPolicyService = new AccessPolicyService(
            provider,
            _settingService,
            _cacheService,
            _cacheInvalidation,
            configuration,
            activityLog);
    }

    [Fact]
    public async Task GetSessionPolicyAsync_UsesConfigFallbackWhenSettingMissing()
    {
        var policy = await _accessPolicyService.GetSessionPolicyAsync();

        Assert.Equal(45, policy.AccessTokenExpirationMinutes);
        Assert.Equal(10, policy.RefreshTokenExpirationDays);
    }

    [Fact]
    public async Task UpdatePasswordPolicyAsync_WithInvalidValues_Throws()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _accessPolicyService.UpdatePasswordPolicyAsync(
                new Settings.Application.AccessPolicy.UpdatePasswordPolicyRequest
                {
                    MinimumLength = 4,
                    RequireUppercase = false,
                    RequireLowercase = false,
                    RequireDigit = false,
                    RequireSpecialCharacter = false,
                    PreventPasswordReuseCount = 0
                }));
    }

    [Fact]
    public async Task UpdateSessionPolicyAsync_InvalidatesPolicyCache()
    {
        await SeedSettingAsync(
            SystemSettingKeys.Session.AccessTokenExpirationMinutes,
            SettingGroups.SessionPolicy,
            SettingDataTypes.Number,
            "30");
        await SeedSettingAsync(
            SystemSettingKeys.Session.RefreshTokenExpirationDays,
            SettingGroups.SessionPolicy,
            SettingDataTypes.Number,
            "7");
        await SeedSettingAsync(
            SystemSettingKeys.Session.RefreshTokenReuseDetectionEnabled,
            SettingGroups.SessionPolicy,
            SettingDataTypes.Boolean,
            "True");
        await SeedSettingAsync(
            SystemSettingKeys.Session.SessionTimeoutMinutes,
            SettingGroups.SessionPolicy,
            SettingDataTypes.Number,
            null);

        await _accessPolicyService.GetSessionPolicyAsync();
        _cacheInvalidation.RemovedKeys.Clear();

        await _accessPolicyService.UpdateSessionPolicyAsync(
            new Settings.Application.AccessPolicy.UpdateSessionPolicyRequest
            {
                AccessTokenExpirationMinutes = 60,
                RefreshTokenExpirationDays = 14,
                RefreshTokenReuseDetectionEnabled = true
            });

        Assert.Contains(CacheKeys.AccessPolicySession, _cacheInvalidation.RemovedKeys);
    }

    private async Task SeedSettingAsync(string key, string group, string dataType, string value)
    {
        var setting = SystemSetting.Create(
            key,
            group,
            key,
            null,
            value,
            value,
            dataType,
            false,
            false,
            true,
            true,
            1,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting);
        await _unitOfWork.SaveChangesAsync();
    }

    public void Dispose() => _dbContext.Dispose();
}

public sealed class PasswordPolicyValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WhenPasswordMeetsPolicy_ReturnsNoErrors()
    {
        var accessPolicy = new FakeAccessPolicyService(new Settings.Application.AccessPolicy.PasswordPolicyResponse
        {
            MinimumLength = 8,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true,
            RequireSpecialCharacter = false
        });

        var validator = new PasswordPolicyValidator(accessPolicy);
        var errors = await validator.ValidateAsync("Password1");

        Assert.Empty(errors);
    }

    [Fact]
    public async Task ValidateAsync_WhenPasswordTooShort_ReturnsError()
    {
        var accessPolicy = new FakeAccessPolicyService(new Settings.Application.AccessPolicy.PasswordPolicyResponse
        {
            MinimumLength = 12,
            RequireUppercase = true,
            RequireLowercase = true,
            RequireDigit = true
        });

        var validator = new PasswordPolicyValidator(accessPolicy);
        var errors = await validator.ValidateAsync("Short1A");

        Assert.Contains(errors, error => error.Contains("at least 12", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class FakeAccessPolicyService(Settings.Application.AccessPolicy.PasswordPolicyResponse policy)
        : Settings.Application.Abstractions.IAccessPolicyService
    {
        public Task<Settings.Application.AccessPolicy.PasswordPolicyResponse> GetPasswordPolicyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(policy);

        public Task UpdatePasswordPolicyAsync(
            Settings.Application.AccessPolicy.UpdatePasswordPolicyRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Settings.Application.AccessPolicy.LoginPolicyResponse> GetLoginPolicyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Settings.Application.AccessPolicy.LoginPolicyResponse());

        public Task UpdateLoginPolicyAsync(
            Settings.Application.AccessPolicy.UpdateLoginPolicyRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Settings.Application.AccessPolicy.SessionPolicyResponse> GetSessionPolicyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Settings.Application.AccessPolicy.SessionPolicyResponse());

        public Task UpdateSessionPolicyAsync(
            Settings.Application.AccessPolicy.UpdateSessionPolicyRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<Settings.Application.AccessPolicy.MaintenancePolicyResponse> GetMaintenancePolicyAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new Settings.Application.AccessPolicy.MaintenancePolicyResponse());

        public Task UpdateMaintenancePolicyAsync(
            Settings.Application.AccessPolicy.UpdateMaintenancePolicyRequest request,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}

public sealed class SettingsSeederTests : IDisposable
{
    private readonly SettingsDbContext _dbContext;
    private readonly SettingsUnitOfWork _unitOfWork;

    public SettingsSeederTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new SettingsDbContext(options, currentUser, dateTime);
        _unitOfWork = new SettingsUnitOfWork(_dbContext, NullLogger<SettingsUnitOfWork>.Instance);
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingValues()
    {
        var existing = SystemSetting.Create(
            SystemSettingKeys.Password.MinimumLength,
            SettingGroups.PasswordPolicy,
            "Minimum Password Length",
            null,
            "16",
            "16",
            SettingDataTypes.Number,
            false,
            false,
            true,
            true,
            1,
            DateTimeOffset.UtcNow);
        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(existing);
        await _unitOfWork.SaveChangesAsync();

        var configuration = new ConfigurationBuilder().Build();
        var seeder = new SettingsSeeder(
            _unitOfWork,
            new FixedDateTimeProvider(DateTimeOffset.UtcNow),
            configuration,
            NullLogger<SettingsSeeder>.Instance);

        await seeder.SeedAsync();

        var stored = await _dbContext.SystemSettings.SingleAsync(s => s.Key == SystemSettingKeys.Password.MinimumLength);
        Assert.Equal("16", stored.Value);
    }

    public void Dispose() => _dbContext.Dispose();
}
