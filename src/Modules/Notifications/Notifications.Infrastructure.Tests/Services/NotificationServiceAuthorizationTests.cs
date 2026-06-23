using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Notifications.Application.Permissions;
using Notifications.Domain.Constants;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Notifications.Infrastructure.Tests.Services;

public sealed class NotificationServiceAuthorizationTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly NotificationsUnitOfWork _unitOfWork;

    public NotificationServiceAuthorizationTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser(TestDataFactory.DefaultUserId);
        var dateTime = TestDataFactory.CreateFixedClock();
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new NotificationsDbContext(options, currentUser, dateTime);
        _unitOfWork = new NotificationsUnitOfWork(_dbContext);
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserReadsOtherNotificationWithoutViewAll_ThrowsForbidden()
    {
        var notification = Notification.Create(
            TestDataFactory.SecondaryUserId,
            "Other user alert",
            "Message",
            NotificationTypes.Info,
            null,
            null,
            null,
            TestDataFactory.CreateFixedClock().UtcNow,
            TestDataFactory.SecondaryUserId);
        await _dbContext.Notifications.AddAsync(notification);
        await _dbContext.SaveChangesAsync();

        var service = CreateService([]);

        var exception = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetByIdAsync(notification.Id));

        Assert.Equal(NotificationErrors.ViewAllRequired, exception.Code);
    }

    private NotificationService CreateService(IReadOnlyCollection<string> permissions)
    {
        var currentUser = new FakeCurrentUserService(
            TestDataFactory.DefaultUserId,
            permissions: permissions);
        var dateTime = TestDataFactory.CreateFixedClock();

        return new NotificationService(
            _unitOfWork,
            currentUser,
            new NotificationAuthorizationService(currentUser),
            dateTime,
            new FakeActivityLogService(),
            new RecordingCacheService(),
            new RecordingCacheInvalidationBuffer(),
            NullLogger<NotificationService>.Instance);
    }

    public void Dispose() => _dbContext.Dispose();
}
