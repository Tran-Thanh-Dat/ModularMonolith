using BuildingBlocks.Application.Errors;
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

public sealed class EmailServiceTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly FakeEmailSender _emailSender;
    private readonly EmailService _service;

    public EmailServiceTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        var dateTime = TestDataFactory.CreateFixedClock();
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new NotificationsDbContext(options, currentUser, dateTime);
        _unitOfWork = new NotificationsUnitOfWork(_dbContext);
        _emailSender = new FakeEmailSender();

        _service = new EmailService(
            _unitOfWork,
            _emailSender,
            new EmailTemplateService(
                _unitOfWork,
                currentUser,
                dateTime,
                new FakeActivityLogService(),
                new RecordingCacheService(),
                new RecordingCacheInvalidationBuffer(),
                NullLogger<EmailTemplateService>.Instance),
            Options.Create(new EmailOptions()),
            currentUser,
            dateTime,
            new FakeActivityLogService(),
            NullLogger<EmailService>.Instance);
    }

    [Fact]
    public async Task SendEmailAsync_WhenTestModeDisabledByDefault_SendsToOriginalRecipient()
    {
        var response = await _service.SendEmailAsync(
            "user@example.com",
            null,
            null,
            "Subject",
            "Body",
            false,
            null,
            null);

        await _unitOfWork.SaveChangesAsync();

        Assert.True(response.IsSuccess);
        Assert.Equal("user@example.com", _emailSender.LastRequest!.To);
        Assert.Equal(EmailStatuses.Sent, response.Status);
    }

    [Fact]
    public async Task SendEmailAsync_WhenTestModeEnabledWithoutRedirect_BlocksSendAndCreatesFailedHistory()
    {
        var service = CreateService(new EmailOptions
        {
            TestMode = new EmailTestModeOptions { Enabled = true, RedirectTo = string.Empty }
        });

        var response = await service.SendEmailAsync(
            "user@example.com",
            null,
            null,
            "Subject",
            "Body",
            false,
            null,
            null);

        await _unitOfWork.SaveChangesAsync();

        Assert.False(response.IsSuccess);
        Assert.Equal(EmailStatuses.Failed, response.Status);
        Assert.Equal("Email provider is not configured.", response.ErrorMessage);
        Assert.Equal(0, _emailSender.SendCount);
    }

    [Fact]
    public async Task SendEmailAsync_WhenTestModeEnabledWithRedirect_RedirectsRecipient()
    {
        var service = CreateService(new EmailOptions
        {
            TestMode = new EmailTestModeOptions { Enabled = true, RedirectTo = "qa@example.com" }
        });

        await service.SendEmailAsync(
            "user@example.com",
            null,
            null,
            "Subject",
            "Body",
            false,
            null,
            null);

        await _unitOfWork.SaveChangesAsync();

        Assert.Equal("qa@example.com", _emailSender.LastRequest!.To);
        Assert.Equal("user@example.com", _emailSender.LastRequest.OriginalTo);
    }

    [Fact]
    public async Task SendEmailAsync_WhenSenderFails_CreatesFailedEmailMessageHistory()
    {
        var sender = new FakeEmailSender
        {
            ShouldFail = true,
            FailureMessage = "SMTP timeout at mail.internal:587"
        };
        var service = CreateService(new EmailOptions(), sender);

        var response = await service.SendEmailAsync(
            "user@example.com",
            null,
            null,
            "Subject",
            "Body",
            false,
            null,
            null);

        await _unitOfWork.SaveChangesAsync();

        Assert.False(response.IsSuccess);
        Assert.Equal(EmailStatuses.Failed, response.Status);
        Assert.Equal(ClientSafeErrorMessages.EmailDeliveryFailed, response.ErrorMessage);

        var saved = await _dbContext.EmailMessages.SingleAsync();
        Assert.Equal(EmailStatuses.Failed, saved.Status);
        Assert.Equal("SMTP timeout at mail.internal:587", saved.ErrorMessage);
    }

    private EmailService CreateService(EmailOptions emailOptions, FakeEmailSender? sender = null)
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        var dateTime = TestDataFactory.CreateFixedClock();

        return new EmailService(
            _unitOfWork,
            sender ?? _emailSender,
            new EmailTemplateService(
                _unitOfWork,
                currentUser,
                dateTime,
                new FakeActivityLogService(),
                new RecordingCacheService(),
                new RecordingCacheInvalidationBuffer(),
                NullLogger<EmailTemplateService>.Instance),
            Options.Create(emailOptions),
            currentUser,
            dateTime,
            new FakeActivityLogService(),
            NullLogger<EmailService>.Instance);
    }

    public void Dispose() => _dbContext.Dispose();
}
