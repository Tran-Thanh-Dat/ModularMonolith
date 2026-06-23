using System.Text.Json;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using Settings.Application.Abstractions;
using Settings.Domain.Settings;
using Settings.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Settings.Infrastructure.Services;

public sealed class SettingProvider : ISettingProvider
{
    private readonly SettingsUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public SettingProvider(
        SettingsUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public Task<string?> GetStringAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default) =>
        ResolveValueAsync(key, defaultValue, cancellationToken);

    public async Task<int> GetIntAsync(
        string key,
        int defaultValue,
        CancellationToken cancellationToken = default)
    {
        var raw = await ResolveValueAsync(key, null, cancellationToken);
        return SettingValueConverter.ParseInt(raw, defaultValue);
    }

    public async Task<bool> GetBoolAsync(
        string key,
        bool defaultValue,
        CancellationToken cancellationToken = default)
    {
        var raw = await ResolveValueAsync(key, null, cancellationToken);
        return SettingValueConverter.ParseBool(raw, defaultValue);
    }

    public async Task<decimal> GetDecimalAsync(
        string key,
        decimal defaultValue,
        CancellationToken cancellationToken = default)
    {
        var raw = await ResolveValueAsync(key, null, cancellationToken);
        return SettingValueConverter.ParseDecimal(raw, defaultValue);
    }

    public async Task<T?> GetJsonAsync<T>(
        string key,
        T? defaultValue = default,
        CancellationToken cancellationToken = default)
        where T : class
    {
        var raw = await ResolveValueAsync(key, null, cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(raw) ?? defaultValue;
        }
        catch (JsonException)
        {
            return defaultValue;
        }
    }

    public async Task<string> GetRequiredAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await ResolveValueAsync(key, cancellationToken: cancellationToken);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new NotFoundException(
                SettingErrors.NotFound,
                $"Required setting '{key}' was not found or has no value.");
        }

        return value;
    }

    private async Task<string?> ResolveValueAsync(
        string key,
        string? defaultValue = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedKey = key.Trim();
        var cacheKey = CacheKeys.SettingByKey(normalizedKey);

        var cached = await _cacheService.GetAsync<SettingValueCacheEntry>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached.Found ? cached.Value : defaultValue;
        }

        var setting = await _unitOfWork.Repository<SystemSetting, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(
                s => s.Key == normalizedKey && !s.IsDeleted && s.IsActive,
                cancellationToken);

        if (setting is null || setting.IsSensitive || setting.IsEncrypted)
        {
            await _cacheService.SetAsync(
                cacheKey,
                SettingValueCacheEntry.NotFound(),
                cancellationToken: cancellationToken);
            return defaultValue;
        }

        var resolved = setting.Value ?? setting.DefaultValue;
        await _cacheService.SetAsync(
            cacheKey,
            SettingValueCacheEntry.WithValue(resolved),
            cancellationToken: cancellationToken);

        return resolved ?? defaultValue;
    }

    private sealed class SettingValueCacheEntry
    {
        public bool Found { get; init; }

        public string? Value { get; init; }

        public static SettingValueCacheEntry NotFound() => new() { Found = false };

        public static SettingValueCacheEntry WithValue(string? value) => new() { Found = true, Value = value };
    }
}
