using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.EmailTemplates;
using Notifications.Infrastructure.Persistence;
using Notifications.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Notifications.Infrastructure.Tests.Services;

public sealed class EmailTemplateServiceTests : IDisposable
{
    private readonly NotificationsDbContext _dbContext;
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly EmailTemplateService _service;

    public EmailTemplateServiceTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        var dateTime = TestDataFactory.CreateFixedClock();
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new NotificationsDbContext(options, currentUser, dateTime);
        _unitOfWork = new NotificationsUnitOfWork(_dbContext);
        _service = new EmailTemplateService(
            _unitOfWork,
            currentUser,
            dateTime,
            new FakeActivityLogService(),
            new RecordingCacheService(),
            new RecordingCacheInvalidationBuffer(),
            NullLogger<EmailTemplateService>.Instance);
    }

    [Fact]
    public async Task RenderTemplateAsync_WhenTemplateInactive_ThrowsTemplateInactive()
    {
        var template = EmailTemplate.Create(
            "welcome",
            "Welcome",
            "Hello",
            "Body",
            false,
            null,
            DateTimeOffset.UtcNow,
            TestDataFactory.DefaultUserId);
        template.Deactivate();
        await _dbContext.EmailTemplates.AddAsync(template);
        await _dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RenderTemplateAsync("welcome", new Dictionary<string, string>()));

        Assert.Equal(EmailErrors.TemplateInactive, exception.Code);
    }

    public void Dispose() => _dbContext.Dispose();
}
