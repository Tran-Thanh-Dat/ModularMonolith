using BuildingBlocks.Application.Pagination;
using Settings.Application.AccessPolicy;
using Settings.Application.Settings;

namespace Settings.Application.Abstractions;

public interface ISettingService
{
    Task<PagedResult<SettingListItemResponse>> GetSettingsAsync(
        string? keyword,
        string? group,
        string? dataType,
        bool? isActive,
        bool? isSystem,
        bool? isEditable,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SettingDetailResponse?> GetByIdAsync(
        Guid id,
        bool includeSensitive,
        CancellationToken cancellationToken = default);

    Task<SettingDetailResponse?> GetByKeyAsync(
        string key,
        bool includeSensitive,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SettingListItemResponse>> GetByGroupAsync(
        string group,
        CancellationToken cancellationToken = default);

    Task<Guid> CreateAsync(
        CreateSettingRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        UpdateSettingRequest request,
        CancellationToken cancellationToken = default);

    Task UpdateValueAsync(
        Guid id,
        string? value,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface ISettingProvider
{
    Task<string?> GetStringAsync(string key, string? defaultValue = null, CancellationToken cancellationToken = default);

    Task<int> GetIntAsync(string key, int defaultValue, CancellationToken cancellationToken = default);

    Task<bool> GetBoolAsync(string key, bool defaultValue, CancellationToken cancellationToken = default);

    Task<decimal> GetDecimalAsync(string key, decimal defaultValue, CancellationToken cancellationToken = default);

    Task<T?> GetJsonAsync<T>(string key, T? defaultValue = default, CancellationToken cancellationToken = default)
        where T : class;

    Task<string> GetRequiredAsync(string key, CancellationToken cancellationToken = default);
}

public interface IAccessPolicyService
{
    Task<PasswordPolicyResponse> GetPasswordPolicyAsync(CancellationToken cancellationToken = default);

    Task UpdatePasswordPolicyAsync(UpdatePasswordPolicyRequest request, CancellationToken cancellationToken = default);

    Task<LoginPolicyResponse> GetLoginPolicyAsync(CancellationToken cancellationToken = default);

    Task UpdateLoginPolicyAsync(UpdateLoginPolicyRequest request, CancellationToken cancellationToken = default);

    Task<SessionPolicyResponse> GetSessionPolicyAsync(CancellationToken cancellationToken = default);

    Task UpdateSessionPolicyAsync(UpdateSessionPolicyRequest request, CancellationToken cancellationToken = default);

    Task<MaintenancePolicyResponse> GetMaintenancePolicyAsync(CancellationToken cancellationToken = default);

    Task UpdateMaintenancePolicyAsync(UpdateMaintenancePolicyRequest request, CancellationToken cancellationToken = default);
}

public interface IPasswordPolicyValidator
{
    Task<IReadOnlyList<string>> ValidateAsync(string password, CancellationToken cancellationToken = default);
}

public interface ISettingsSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
