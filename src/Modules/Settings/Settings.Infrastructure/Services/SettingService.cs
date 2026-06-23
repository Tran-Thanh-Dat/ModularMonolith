using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Settings.Application.Abstractions;
using Settings.Application.Settings;
using Settings.Domain.Constants;
using Settings.Domain.Settings;
using Settings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Settings.Infrastructure.Services;

public sealed class SettingService : ISettingService
{
    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<SettingService> _logger;

    public SettingService(
        SettingsUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<SettingService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheService = cacheService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<PagedResult<SettingListItemResponse>> GetSettingsAsync(
        string? keyword,
        string? group,
        string? dataType,
        bool? isActive,
        bool? isSystem,
        bool? isEditable,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var queryHash = CacheKeys.HashQueryParameters(
            keyword,
            group,
            dataType,
            isActive,
            isSystem,
            isEditable,
            pageIndex,
            pageSize);

        var cacheKey = CacheKeys.SettingList(queryHash);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
                var query = _unitOfWork.Repository<SystemSetting, Guid>()
                    .QueryReadOnly()
                    .Where(s => !s.IsDeleted);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var pattern = $"%{keyword.Trim()}%";
                    query = query.Where(s =>
                        EF.Functions.ILike(s.Key, pattern) ||
                        EF.Functions.ILike(s.Name, pattern) ||
                        EF.Functions.ILike(s.Group, pattern) ||
                        EF.Functions.ILike(s.Description ?? string.Empty, pattern));
                }

                if (!string.IsNullOrWhiteSpace(group))
                {
                    query = query.Where(s => s.Group == group.Trim());
                }

                if (!string.IsNullOrWhiteSpace(dataType))
                {
                    query = query.Where(s => s.DataType == dataType.Trim());
                }

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                if (isSystem.HasValue)
                {
                    query = query.Where(s => s.IsSystem == isSystem.Value);
                }

                if (isEditable.HasValue)
                {
                    query = query.Where(s => s.IsEditable == isEditable.Value);
                }

                var projected = query
                    .OrderBy(s => s.SortOrder)
                    .ThenByDescending(s => s.CreatedAt)
                    .Select(s => new SettingListItemResponse
                    {
                        Id = s.Id,
                        Key = s.Key,
                        Group = s.Group,
                        Name = s.Name,
                        DataType = s.DataType,
                        IsSensitive = s.IsSensitive,
                        IsEncrypted = s.IsEncrypted,
                        IsSystem = s.IsSystem,
                        IsActive = s.IsActive,
                        IsEditable = s.IsEditable,
                        SortOrder = s.SortOrder,
                        CreatedAt = s.CreatedAt
                    });

                return await projected.ToPagedResultAsync(pagedRequest, ct);
            },
            cancellationToken: cancellationToken);
    }

    public async Task<SettingDetailResponse?> GetByIdAsync(
        Guid id,
        bool includeSensitive,
        CancellationToken cancellationToken = default)
    {
        if (!includeSensitive)
        {
            var cacheKey = CacheKeys.SettingDetail(id);
            var cached = await _cacheService.GetAsync<SettingDetailResponse>(cacheKey, cancellationToken);
            if (cached is not null)
            {
                return cached;
            }
        }

        var setting = await _unitOfWork.Repository<SystemSetting, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (setting is null)
        {
            return null;
        }

        var detail = MapDetail(setting, includeSensitive);

        if (!includeSensitive && !setting.IsSensitive && !setting.IsEncrypted)
        {
            await _cacheService.SetAsync(
                CacheKeys.SettingDetail(id),
                detail,
                cancellationToken: cancellationToken);
        }

        return detail;
    }

    public async Task<SettingDetailResponse?> GetByKeyAsync(
        string key,
        bool includeSensitive,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();

        var setting = await _unitOfWork.Repository<SystemSetting, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(s => s.Key == normalizedKey && !s.IsDeleted, cancellationToken);

        if (setting is null)
        {
            return null;
        }

        return MapDetail(setting, includeSensitive);
    }

    public async Task<IReadOnlyCollection<SettingListItemResponse>> GetByGroupAsync(
        string group,
        CancellationToken cancellationToken = default)
    {
        var normalizedGroup = group.Trim();
        var cacheKey = CacheKeys.SettingGroup(normalizedGroup);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                return await _unitOfWork.Repository<SystemSetting, Guid>()
                    .QueryReadOnly()
                    .Where(s => !s.IsDeleted && s.Group == normalizedGroup)
                    .OrderBy(s => s.SortOrder)
                    .ThenByDescending(s => s.CreatedAt)
                    .Select(s => new SettingListItemResponse
                    {
                        Id = s.Id,
                        Key = s.Key,
                        Group = s.Group,
                        Name = s.Name,
                        DataType = s.DataType,
                        IsSensitive = s.IsSensitive,
                        IsEncrypted = s.IsEncrypted,
                        IsSystem = s.IsSystem,
                        IsActive = s.IsActive,
                        IsEditable = s.IsEditable,
                        SortOrder = s.SortOrder,
                        CreatedAt = s.CreatedAt
                    })
                    .ToListAsync(ct);
            },
            cancellationToken: cancellationToken);
    }

    public async Task<Guid> CreateAsync(
        CreateSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!SettingDataTypes.All.Contains(request.DataType))
        {
            throw new BadRequestException(
                SettingErrors.InvalidDataType,
                $"Data type '{request.DataType}' is not supported.");
        }

        SettingValueConverter.ValidateValue(request.DataType, request.Value);
        SettingValueConverter.ValidateValue(request.DataType, request.DefaultValue);

        var normalizedKey = request.Key.Trim();
        if (await KeyExistsAsync(normalizedKey, null, cancellationToken))
        {
            throw new ConflictException(
                SettingErrors.KeyAlreadyExists,
                $"Setting key '{normalizedKey}' already exists.");
        }

        var setting = SystemSetting.Create(
            normalizedKey,
            request.Group,
            request.Name,
            request.Description,
            request.Value,
            request.DefaultValue,
            request.DataType,
            request.IsEncrypted,
            request.IsSensitive,
            request.IsSystem,
            request.IsEditable,
            request.SortOrder,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<SystemSetting, Guid>().AddAsync(setting, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created setting: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);

        _logger.LogInformation("Created setting {SettingId} {Key}", setting.Id, setting.Key);

        return setting.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        UpdateSettingRequest request,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingForUpdateAsync(id, cancellationToken);
        setting.UpdateMetadata(request.Name, request.Description, request.SortOrder, request.IsEditable);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated setting metadata: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);

        _logger.LogInformation("Updated setting metadata {SettingId}", id);
    }

    public async Task UpdateValueAsync(
        Guid id,
        string? value,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingForUpdateAsync(id, cancellationToken);
        SettingValueConverter.ValidateValue(setting.DataType, value);

        try
        {
            setting.UpdateValue(value);
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException exception)
        {
            throw new BadRequestException(
                SettingErrors.NotEditable,
                exception.Message);
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated setting value: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);
        InvalidateAccessPolicyCaches(setting.Key);

        _logger.LogInformation("Updated setting value {SettingId}", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingForUpdateAsync(id, cancellationToken);

        try
        {
            setting.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new BadRequestException(
                SettingErrors.SystemSettingCannotBeDeleted,
                "System settings cannot be deleted.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted setting: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);
        InvalidateAccessPolicyCaches(setting.Key);

        _logger.LogInformation("Soft-deleted setting {SettingId}", id);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingForUpdateAsync(id, cancellationToken);

        try
        {
            setting.Activate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(
                SettingErrors.InvalidValue,
                $"Setting '{setting.Key}' is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated setting: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);

        _logger.LogInformation("Activated setting {SettingId}", id);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingForUpdateAsync(id, cancellationToken);

        try
        {
            setting.Deactivate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(
                SettingErrors.InvalidValue,
                $"Setting '{setting.Key}' is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated setting: {setting.Key}",
            AuditLogConstants.Modules.Settings,
            cancellationToken: cancellationToken);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);
        InvalidateAccessPolicyCaches(setting.Key);

        _logger.LogInformation("Deactivated setting {SettingId}", id);
    }

    internal async Task UpdateSettingValueByKeyAsync(
        string key,
        string? value,
        CancellationToken cancellationToken = default)
    {
        var setting = await _unitOfWork.Repository<SystemSetting, Guid>()
            .Query()
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted, cancellationToken);

        if (setting is null)
        {
            throw new NotFoundException(
                SettingErrors.NotFound,
                $"Setting with key '{key}' was not found.");
        }

        SettingValueConverter.ValidateValue(setting.DataType, value);
        setting.UpdateValue(value);

        InvalidateSettingCache(setting.Key, setting.Group, setting.Id);
        InvalidateAccessPolicyCaches(setting.Key);
    }

    private async Task<SystemSetting> GetSettingForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var setting = await _unitOfWork.Repository<SystemSetting, Guid>()
            .Query()
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, cancellationToken);

        if (setting is null)
        {
            throw new NotFoundException(
                SettingErrors.NotFound,
                $"Setting with id '{id}' was not found.");
        }

        return setting;
    }

    private async Task<bool> KeyExistsAsync(
        string key,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<SystemSetting, Guid>()
            .QueryReadOnly()
            .Where(s => !s.IsDeleted && s.Key == key);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static SettingDetailResponse MapDetail(SystemSetting setting, bool includeSensitive)
    {
        var isMasked = false;
        string? value = setting.Value;
        string? defaultValue = setting.DefaultValue;

        if (setting.IsEncrypted && !includeSensitive)
        {
            value = null;
            defaultValue = null;
            isMasked = true;
        }
        else if (setting.IsSensitive && !includeSensitive)
        {
            value = SettingValueConverter.MaskSensitive(value);
            defaultValue = SettingValueConverter.MaskSensitive(defaultValue);
            isMasked = true;
        }

        return new SettingDetailResponse
        {
            Id = setting.Id,
            Key = setting.Key,
            Group = setting.Group,
            Name = setting.Name,
            Description = setting.Description,
            Value = value,
            DefaultValue = defaultValue,
            DataType = setting.DataType,
            IsEncrypted = setting.IsEncrypted,
            IsSensitive = setting.IsSensitive,
            IsSystem = setting.IsSystem,
            IsActive = setting.IsActive,
            IsEditable = setting.IsEditable,
            SortOrder = setting.SortOrder,
            IsValueMasked = isMasked,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt
        };
    }

    private void InvalidateSettingCache(string key, string group, Guid id)
    {
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.SettingDetail(id));
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.SettingByKey(key));
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.SettingGroup(group));
        _cacheInvalidationBuffer.EnqueueRemoveByPrefix(CacheKeys.SettingListPrefix);
    }

    private void InvalidateAccessPolicyCaches(string key)
    {
        if (key.StartsWith("Security.Password.", StringComparison.Ordinal))
        {
            _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicyPassword);
        }
        else if (key.StartsWith("Security.Login.", StringComparison.Ordinal))
        {
            _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicyLogin);
        }
        else if (key.StartsWith("Security.Session.", StringComparison.Ordinal))
        {
            _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.AccessPolicySession);
        }
        else if (key.StartsWith("Maintenance.", StringComparison.Ordinal))
        {
            _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.MaintenancePolicy);
        }
    }
}
