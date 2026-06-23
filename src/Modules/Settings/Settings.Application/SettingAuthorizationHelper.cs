using BuildingBlocks.Application.Abstractions;
using Settings.Application.Permissions;

namespace Settings.Application;

internal static class SettingAuthorizationHelper
{
    public static bool CanViewSensitive(ICurrentUserService currentUserService) =>
        currentUserService.Permissions.Contains(
            SettingsPermissionCodes.ViewSensitive,
            StringComparer.OrdinalIgnoreCase);
}
