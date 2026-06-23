using System.Text.Json;
using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailMessages;
using Notifications.Application.Options;
using Notifications.Domain.Constants;
using Notifications.Domain.EmailMessages;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SendEmailRequest = Notifications.Application.Abstractions.SendEmailRequest;

namespace Notifications.Infrastructure.Services;

public sealed class EmailService : IEmailService
{
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly EmailOptions _emailOptions;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        NotificationsUnitOfWork unitOfWork,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        IOptions<EmailOptions> emailOptions,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<EmailService> logger)
    {
        _unitOfWork = unitOfWork;
        _emailSender = emailSender;
        _emailTemplateService = emailTemplateService;
        _emailOptions = emailOptions.Value;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public Task<SendEmailResponse> SendEmailAsync(
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        string? referenceType,
        string? referenceId,
        CancellationToken cancellationToken = default) =>
        SendInternalAsync(to, cc, bcc, subject, body, isHtml, null, null, referenceType, referenceId, cancellationToken);

    public async Task<SendEmailResponse> SendTemplateEmailAsync(
        string templateCode,
        string to,
        string? cc,
        string? bcc,
        IReadOnlyDictionary<string, string> templateData,
        string? referenceType,
        string? referenceId,
        CancellationToken cancellationToken = default)
    {
        var rendered = await _emailTemplateService.RenderTemplateAsync(templateCode, templateData, cancellationToken);
        var templateDataJson = JsonSerializer.Serialize(templateData);

        return await SendInternalAsync(
            to,
            cc,
            bcc,
            rendered.Subject,
            rendered.Body,
            rendered.IsHtml,
            templateCode,
            templateDataJson,
            referenceType,
            referenceId,
            cancellationToken);
    }

    public async Task<EmailMessageDetailResponse?> GetEmailMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var message = await _unitOfWork.Repository<EmailMessage, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted, cancellationToken);

        return message is null ? null : MapDetail(message);
    }

    public async Task<PagedResult<EmailMessageListItemResponse>> GetEmailMessagesAsync(
        string? keyword,
        string? to,
        string? status,
        string? provider,
        string? templateCode,
        string? referenceType,
        string? referenceId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<EmailMessage, Guid>().QueryReadOnly().Where(m => !m.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(m =>
                EF.Functions.ILike(m.To, pattern) ||
                EF.Functions.ILike(m.Subject, pattern) ||
                EF.Functions.ILike(m.Body, pattern) ||
                EF.Functions.ILike(m.TemplateCode ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(to))
        {
            query = query.Where(m => m.To == to.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(m => m.Status == status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(provider))
        {
            query = query.Where(m => m.Provider == provider.Trim());
        }

        if (!string.IsNullOrWhiteSpace(templateCode))
        {
            query = query.Where(m => m.TemplateCode == templateCode.Trim());
        }

        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            query = query.Where(m => m.ReferenceType == referenceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            query = query.Where(m => m.ReferenceId == referenceId.Trim());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new EmailMessageListItemResponse
            {
                Id = m.Id,
                To = m.To,
                Subject = m.Subject,
                Status = m.Status,
                TemplateCode = m.TemplateCode,
                Provider = m.Provider,
                SentAt = m.SentAt,
                CreatedAt = m.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    private async Task<SendEmailResponse> SendInternalAsync(
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        string? templateCode,
        string? templateDataJson,
        string? referenceType,
        string? referenceId,
        CancellationToken cancellationToken)
    {
        if (_emailOptions.TestMode.Enabled &&
            string.IsNullOrWhiteSpace(_emailOptions.TestMode.RedirectTo))
        {
            _logger.LogWarning(
                "Email TestMode is enabled but RedirectTo is not configured. Send blocked to prevent accidental delivery.");

            var blockedMessage = EmailMessage.CreatePending(
                to,
                cc,
                bcc,
                subject,
                body,
                isHtml,
                templateCode,
                templateDataJson,
                _emailOptions.Provider,
                referenceType,
                referenceId,
                _dateTimeProvider.UtcNow,
                _currentUserService.UserId);

            await _unitOfWork.Repository<EmailMessage, Guid>().AddAsync(blockedMessage, cancellationToken);
            blockedMessage.MarkFailed("Email TestMode is enabled but RedirectTo is not configured.");

            await _activityLogService.LogImmediateAsync(
                ActivityTypes.SystemError,
                $"Email send blocked: TestMode enabled without RedirectTo for {blockedMessage.To}",
                AuditLogConstants.Modules.Notifications,
                "Failed",
                "Email provider is not configured.",
                cancellationToken: cancellationToken);

            return new SendEmailResponse
            {
                EmailMessageId = blockedMessage.Id,
                IsSuccess = false,
                Status = blockedMessage.Status,
                ErrorMessage = "Email provider is not configured.",
                SentAt = blockedMessage.SentAt
            };
        }

        var emailMessage = EmailMessage.CreatePending(
            to,
            cc,
            bcc,
            subject,
            body,
            isHtml,
            templateCode,
            templateDataJson,
            _emailOptions.Provider,
            referenceType,
            referenceId,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<EmailMessage, Guid>().AddAsync(emailMessage, cancellationToken);

        var sendRequest = ApplyTestMode(new SendEmailRequest
        {
            To = to,
            Cc = cc,
            Bcc = bcc,
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        });

        var sendResult = await _emailSender.SendAsync(sendRequest, cancellationToken);

        if (sendResult.IsSuccess)
        {
            emailMessage.MarkSent(sendResult.Provider, sendResult.SentAt ?? _dateTimeProvider.UtcNow);

            await _activityLogService.EnqueuePostCommitAsync(
                ActivityTypes.Create,
                $"Email sent: {emailMessage.Subject} to {emailMessage.To}",
                AuditLogConstants.Modules.Notifications,
                cancellationToken: cancellationToken);
        }
        else
        {
            var internalError = sendResult.ErrorMessage;
            emailMessage.MarkFailed(internalError);

            await _activityLogService.LogImmediateAsync(
                ActivityTypes.SystemError,
                $"Email send failed: {emailMessage.Subject} to {emailMessage.To}",
                AuditLogConstants.Modules.Notifications,
                "Failed",
                internalError,
                cancellationToken: cancellationToken);
        }

        return new SendEmailResponse
        {
            EmailMessageId = emailMessage.Id,
            IsSuccess = sendResult.IsSuccess,
            Status = emailMessage.Status,
            ErrorMessage = sendResult.IsSuccess ? null : ClientSafeErrorMessages.EmailDeliveryFailed,
            SentAt = emailMessage.SentAt
        };
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

    private static EmailMessageDetailResponse MapDetail(EmailMessage message) =>
        new()
        {
            Id = message.Id,
            To = message.To,
            Cc = message.Cc,
            Bcc = message.Bcc,
            Subject = message.Subject,
            Body = message.Body,
            IsHtml = message.IsHtml,
            Status = message.Status,
            TemplateCode = message.TemplateCode,
            TemplateData = message.TemplateData,
            Provider = message.Provider,
            ErrorMessage = message.ErrorMessage,
            SentAt = message.SentAt,
            RetryCount = message.RetryCount,
            NextRetryAt = message.NextRetryAt,
            ReferenceType = message.ReferenceType,
            ReferenceId = message.ReferenceId,
            CreatedAt = message.CreatedAt,
            CreatedBy = message.CreatedBy,
            UpdatedAt = message.UpdatedAt,
            UpdatedBy = message.UpdatedBy
        };
}