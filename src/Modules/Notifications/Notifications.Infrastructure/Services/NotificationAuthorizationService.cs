using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using Notifications.Application.Abstractions;
using Notifications.Application.Permissions;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationAuthorizationService : INotificationAuthorizationService
{
    private readonly ICurrentUserService _currentUserService;

    public NotificationAuthorizationService(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public Guid ResolveListUserId(Guid? requestedUserId)
    {
        EnsureAuthenticated();

        var currentUserId = _currentUserService.UserId!.Value;

        if (!requestedUserId.HasValue || requestedUserId.Value == currentUserId)
        {
            return requestedUserId ?? currentUserId;
        }

        if (!HasPermission(NotificationsPermissionCodes.NotificationViewAll))
        {
            throw new ForbiddenException(
                NotificationErrors.ViewAllRequired,
                "Notification.ViewAll permission is required to view another user's notifications.");
        }

        return requestedUserId.Value;
    }

    public void EnsureCanView(Guid notificationUserId)
    {
        EnsureAuthenticated();

        if (notificationUserId == _currentUserService.UserId!.Value)
        {
            return;
        }

        if (!HasPermission(NotificationsPermissionCodes.NotificationViewAll))
        {
            throw new ForbiddenException(
                NotificationErrors.ViewAllRequired,
                "Notification.ViewAll permission is required to view this notification.");
        }
    }

    public void EnsureCanManage(Guid notificationUserId)
    {
        EnsureAuthenticated();

        if (notificationUserId == _currentUserService.UserId!.Value)
        {
            return;
        }

        if (!HasPermission(NotificationsPermissionCodes.NotificationManage))
        {
            throw new ForbiddenException(
                NotificationErrors.ManageRequired,
                "Notification.Manage permission is required to modify another user's notification.");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUserService.IsAuthenticated || !_currentUserService.UserId.HasValue)
        {
            throw new UnauthorizedException(
                CommonErrors.Unauthorized,
                "Authentication is required.");
        }
    }

    private bool HasPermission(string permissionCode) =>
        _currentUserService.Permissions.Contains(permissionCode, StringComparer.Ordinal);
}
