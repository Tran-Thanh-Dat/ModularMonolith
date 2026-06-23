using System.Security.Cryptography;
using System.Text;

namespace BuildingBlocks.Application.Caching;

public static class CacheKeys
{
    public const string Version = "v1";

    public static string UserPermissions(Guid userId) =>
        Build("user", userId.ToString("D"), "permissions");

    public static string CategoryDetail(Guid id) =>
        Build("categories", "detail", id.ToString("D"));

    public static string CategoryList(string queryHash) =>
        Build("categories", "list", queryHash);

    public static string CategoryListPrefix =>
        BuildPrefix("categories", "list");

    public static string EmailTemplateByCode(string code) =>
        Build("email-templates", "code", NormalizeSegment(code));

    public static string EmailTemplateListPrefix =>
        BuildPrefix("email-templates", "list");

    public static string FileResourceDetail(Guid id) =>
        Build("files", "detail", id.ToString("D"));

    public static string NotificationUnreadCount(Guid userId) =>
        Build("notifications", "unread-count", userId.ToString("D"));

    public static string SettingByKey(string key) =>
        Build("settings", "key", NormalizeSegment(key));

    public static string SettingDetail(Guid id) =>
        Build("settings", "detail", id.ToString("D"));

    public static string SettingGroup(string group) =>
        Build("settings", "group", NormalizeSegment(group));

    public static string SettingList(string queryHash) =>
        Build("settings", "list", queryHash);

    public static string SettingListPrefix =>
        BuildPrefix("settings", "list");

    public static string AccessPolicyPassword =>
        Build("access-policy", "password");

    public static string AccessPolicyLogin =>
        Build("access-policy", "login");

    public static string AccessPolicySession =>
        Build("access-policy", "session");

    public static string MaintenancePolicy =>
        Build("access-policy", "maintenance");

    public static string HashQueryParameters(params object?[] parts)
    {
        var raw = string.Join('|', parts.Select(part => part?.ToString() ?? "null"));
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    public static string BuildPrefix(params string[] segments) =>
        string.Join(':', new[] { Version }.Concat(segments));

    public static string Build(params string[] segments) =>
        $"{BuildPrefix(segments)}";

    public static string ApplyConfiguredPrefix(string key, string keyPrefix) =>
        string.IsNullOrWhiteSpace(keyPrefix)
            ? key
            : $"{keyPrefix.Trim().TrimEnd(':')}:{key}";

    public static string ApplyInstanceName(string key, string? instanceName) =>
        string.IsNullOrWhiteSpace(instanceName)
            ? key
            : $"{instanceName}{key}";

    private static string NormalizeSegment(string value) =>
        value.Trim().ToLowerInvariant();
}
