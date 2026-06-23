using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Notifications.Application.Permissions;
using Notifications.Infrastructure.Services;
using Xunit;

namespace Notifications.Infrastructure.Tests.Services;

public sealed class NotificationAuthorizationServiceTests
{
    private readonly Guid _currentUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private readonly Guid _otherUserId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Fact]
    public void ResolveListUserId_WhenUserRequestsOwnNotifications_ReturnsCurrentUserId()
    {
        var service = CreateService([]);

        var resolved = service.ResolveListUserId(_currentUserId);

        Assert.Equal(_currentUserId, resolved);
    }

    [Fact]
    public void ResolveListUserId_WhenUserRequestsOtherUserWithoutViewAll_ReturnsForbidden()
    {
        var service = CreateService([]);

        var exception = Assert.Throws<ForbiddenException>(() =>
            service.ResolveListUserId(_otherUserId));

        Assert.Equal(NotificationErrors.ViewAllRequired, exception.Code);
    }

    [Fact]
    public void ResolveListUserId_WhenAdminHasViewAll_ReturnsRequestedUserId()
    {
        var service = CreateService([NotificationsPermissionCodes.NotificationViewAll]);

        var resolved = service.ResolveListUserId(_otherUserId);

        Assert.Equal(_otherUserId, resolved);
    }

    [Fact]
    public void EnsureCanView_WhenUserReadsOwnNotification_Succeeds()
    {
        var service = CreateService([]);

        var exception = Record.Exception(() => service.EnsureCanView(_currentUserId));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureCanView_WhenUserReadsOtherNotificationWithoutViewAll_ReturnsForbidden()
    {
        var service = CreateService([]);

        var exception = Assert.Throws<ForbiddenException>(() =>
            service.EnsureCanView(_otherUserId));

        Assert.Equal(NotificationErrors.ViewAllRequired, exception.Code);
    }

    [Fact]
    public void EnsureCanManage_WhenUserUpdatesOtherNotificationWithoutManage_ReturnsForbidden()
    {
        var service = CreateService([]);

        var exception = Assert.Throws<ForbiddenException>(() =>
            service.EnsureCanManage(_otherUserId));

        Assert.Equal(NotificationErrors.ManageRequired, exception.Code);
    }

    [Fact]
    public void EnsureCanManage_WhenAdminHasManage_Succeeds()
    {
        var service = CreateService([NotificationsPermissionCodes.NotificationManage]);

        var exception = Record.Exception(() => service.EnsureCanManage(_otherUserId));

        Assert.Null(exception);
    }

    private NotificationAuthorizationService CreateService(IReadOnlyCollection<string> permissions) =>
        new(new FakeCurrentUserService(
            userId: _currentUserId,
            permissions: permissions));
}
