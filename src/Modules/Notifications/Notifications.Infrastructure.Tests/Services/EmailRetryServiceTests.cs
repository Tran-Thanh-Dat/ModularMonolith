using BuildingBlocks.Testing.Fakes;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.EmailMessages;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Services;
using Notifications.Infrastructure.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Notifications.Infrastructure.Tests.Services;

public sealed class EmailRetryServiceTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly FixedDateTimeProvider _dateTime;

    public EmailRetryServiceTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        _dateTime = TestDataFactory.CreateFixedClock(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new NotificationsDbContext(options, currentUser, _dateTime);
        _unitOfWork = new NotificationsUnitOfWork(_dbContext);
    }

    [Fact]
    public async Task ProcessBatchAsync_PicksPendingAndFailedMessages()
    {
        await SeedPendingMessage();
        await SeedRetryableFailedMessage(retryCount: 1, nextRetryAt: _dateTime.UtcNow.AddMinutes(-5));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().ProcessBatchAsync(batchSize: 10, maxRetryCount: 5);

        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(2, result.SuccessCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_RespectsMaxRetryCount()
    {
        await SeedRetryableFailedMessage(retryCount: 5, nextRetryAt: _dateTime.UtcNow.AddMinutes(-5));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().ProcessBatchAsync(batchSize: 10, maxRetryCount: 5);

        Assert.Equal(0, result.ProcessedCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_RespectsNextRetryAt()
    {
        await SeedRetryableFailedMessage(retryCount: 1, nextRetryAt: _dateTime.UtcNow.AddHours(1));
        await _dbContext.SaveChangesAsync();

        var result = await CreateService().ProcessBatchAsync(batchSize: 10, maxRetryCount: 5);

        Assert.Equal(0, result.ProcessedCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenSendSucceeds_MarksSent()
    {
        var message = await SeedPendingMessage();
        await _dbContext.SaveChangesAsync();

        await CreateService().ProcessBatchAsync(batchSize: 10, maxRetryCount: 5);
        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.EmailMessages.SingleAsync(m => m.Id == message.Id);
        Assert.Equal(EmailStatuses.Sent, updated.Status);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenSendFails_IncrementsRetryCountAndSchedulesNextRetryAt()
    {
        var message = await SeedPendingMessage();
        await _dbContext.SaveChangesAsync();

        await CreateService(new FakeEmailSender { ShouldFail = true, FailureMessage = "Delivery failed." })
            .ProcessBatchAsync(batchSize: 10, maxRetryCount: 5);
        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.EmailMessages.SingleAsync(m => m.Id == message.Id);
        Assert.Equal(1, updated.RetryCount);
        Assert.NotNull(updated.NextRetryAt);
        Assert.True(updated.NextRetryAt > _dateTime.UtcNow);
    }

    private EmailRetryService CreateService(FakeEmailSender? sender = null) =>
        new(
            _unitOfWork,
            sender ?? new FakeEmailSender(),
            Options.Create(new EmailOptions()),
            _dateTime,
            new FakeActivityLogService(),
            NullLogger<EmailRetryService>.Instance);

    private async Task<EmailMessage> SeedPendingMessage()
    {
        var message = CreateBaseMessage();
        await _dbContext.EmailMessages.AddAsync(message);
        return message;
    }

    private async Task<EmailMessage> SeedRetryableFailedMessage(int retryCount, DateTimeOffset? nextRetryAt)
    {
        var message = CreateBaseMessage();
        message.MarkFailed("Previous failure");

        for (var i = 0; i < retryCount; i++)
        {
            message.ScheduleRetry(nextRetryAt ?? _dateTime.UtcNow.AddMinutes(-5));
        }

        await _dbContext.EmailMessages.AddAsync(message);
        return message;
    }

    private EmailMessage CreateBaseMessage() =>
        EmailMessage.CreatePending(
            "user@example.com",
            null,
            null,
            "Subject",
            "Body",
            false,
            null,
            null,
            "Fake",
            null,
            null,
            _dateTime.UtcNow,
            TestDataFactory.DefaultUserId);

    public void Dispose() => _dbContext.Dispose();
}
