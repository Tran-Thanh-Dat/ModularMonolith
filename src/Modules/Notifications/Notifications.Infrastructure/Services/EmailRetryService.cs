using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.EmailMessages;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendEmailRequest = Notifications.Application.Abstractions.SendEmailRequest;

namespace Notifications.Infrastructure.Services;

public sealed class EmailRetryService : IEmailRetryService
{
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<EmailRetryService> _logger;

    public EmailRetryService(
        NotificationsUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<EmailRetryService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<EmailRetryBatchResult> ProcessBatchAsync(
        int batchSize,
        int maxRetryCount,
        CancellationToken cancellationToken = default)
    {
        if (_emailOptions.TestMode.Enabled &&
            string.IsNullOrWhiteSpace(_emailOptions.TestMode.RedirectTo))
        {
            _logger.LogWarning("Email retry skipped: TestMode enabled without RedirectTo.");
            return new EmailRetryBatchResult { SkippedCount = batchSize };
        }

        var now = _dateTimeProvider.UtcNow;
        var messages = await _unitOfWork.Repository<EmailMessage, Guid>()
            .Query()
            .Where(message =>
                !message.IsDeleted &&
                (message.Status == EmailStatuses.Pending || message.Status == EmailStatuses.Failed) &&
                message.RetryCount < maxRetryCount &&
                (message.NextRetryAt == null || message.NextRetryAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var successCount = 0;
        var failedCount = 0;

        foreach (var message in messages)
        {
            var sendRequest = ApplyTestMode(new SendEmailRequest
            {
                To = message.To,
                Cc = message.Cc,
                Bcc = message.Bcc,
                Subject = message.Subject,
                Body = message.Body,
                IsHtml = message.IsHtml
            });

            try
            {
                var sendResult = await _emailSender.SendAsync(sendRequest, cancellationToken);

                if (sendResult.IsSuccess)
                {
                    message.MarkSent(sendResult.Provider, sendResult.SentAt ?? now);
                    successCount++;
                }
                else if (message.RetryCount + 1 >= maxRetryCount)
                {
                    message.MarkFailed(sendResult.ErrorMessage);
                    failedCount++;

                    await _activityLogService.LogImmediateAsync(
                        ActivityTypes.SystemError,
                        $"Email retry exhausted: {message.Subject} to {message.To}",
                        AuditLogConstants.Modules.Notifications,
                        "Failed",
                        sendResult.ErrorMessage,
                        cancellationToken: cancellationToken);
                }
                else
                {
                    message.ScheduleRetry(CalculateNextRetryAt(message.RetryCount));
                    failedCount++;

                    await _activityLogService.LogImmediateAsync(
                        ActivityTypes.SystemError,
                        $"Email retry failed: {message.Subject} to {message.To}",
                        AuditLogConstants.Modules.Notifications,
                        "Failed",
                        sendResult.ErrorMessage,
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Email retry exception for message {EmailMessageId}",
                    message.Id);

                var safeMessage = "Email delivery failed.";

                if (message.RetryCount + 1 >= maxRetryCount)
                {
                    message.MarkFailed(safeMessage);
                }
                else
                {
                    message.ScheduleRetry(CalculateNextRetryAt(message.RetryCount));
                }

                failedCount++;

                await _activityLogService.LogImmediateAsync(
                    ActivityTypes.SystemError,
                    $"Email retry exception: {message.Subject} to {message.To}",
                    AuditLogConstants.Modules.Notifications,
                    "Failed",
                    safeMessage,
                    cancellationToken: cancellationToken);
            }
        }

        return new EmailRetryBatchResult
        {
            ProcessedCount = messages.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = 0
        };
    }

    private DateTimeOffset CalculateNextRetryAt(int currentRetryCount)
    {
        var exponent = Math.Min(currentRetryCount, 5);
        var delayMinutes = Math.Min(120, 5 * Math.Pow(2, exponent));
        return _dateTimeProvider.UtcNow.AddMinutes(delayMinutes);
    }

    private SendEmailRequest ApplyTestMode(SendEmailRequest request)
    {
        if (!_emailOptions.TestMode.Enabled ||
            string.IsNullOrWhiteSpace(_emailOptions.TestMode.RedirectTo))
        {
            return request;
        }

        return new SendEmailRequest
        {
            To = _emailOptions.TestMode.RedirectTo.Trim(),
            Cc = null,
            Bcc = null,
            Subject = request.Subject,
            Body = request.Body,
            IsHtml = request.IsHtml,
            OriginalTo = request.To,
            OriginalCc = request.Cc,
            OriginalBcc = request.Bcc
        };
    }
}
