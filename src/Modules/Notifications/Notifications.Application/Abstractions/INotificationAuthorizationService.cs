using BuildingBlocks.Application.Abstractions;

namespace Notifications.Application.Abstractions;

public interface INotificationAuthorizationService
{
    Guid ResolveListUserId(Guid? requestedUserId);

    void EnsureCanView(Guid notificationUserId);

    void EnsureCanManage(Guid notificationUserId);
}
