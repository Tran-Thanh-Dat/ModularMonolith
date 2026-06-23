using BuildingBlocks.Application.Pagination;
using Notifications.Application.EmailMessages;
using Notifications.Application.EmailTemplates;
using Notifications.Application.Notifications;

namespace Notifications.Application.Abstractions;

public interface IEmailService
{
    Task<SendEmailResponse> SendEmailAsync(
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        string? referenceType,
        string? referenceId,
        CancellationToken cancellationToken = default);

    Task<SendEmailResponse> SendTemplateEmailAsync(
        string templateCode,
        string to,
        string? cc,
        string? bcc,
        IReadOnlyDictionary<string, string> templateData,
        string? referenceType,
        string? referenceId,
        CancellationToken cancellationToken = default);

    Task<EmailMessageDetailResponse?> GetEmailMessageByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<EmailMessageListItemResponse>> GetEmailMessagesAsync(
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
        CancellationToken cancellationToken = default);
}

public interface IEmailTemplateService
{
    Task<Guid> CreateAsync(
        string code,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmailTemplateDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<EmailTemplateListItemResponse>> GetListAsync(
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<(string Subject, string Body, bool IsHtml)> RenderTemplateAsync(
        string templateCode,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<Guid> CreateAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string? referenceType,
        string? referenceId,
        string? metadata,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default);

    Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default);

    Task<NotificationDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<NotificationListItemResponse>> GetListAsync(
        Guid userId,
        string? keyword,
        string? type,
        string? status,
        bool? isRead,
        string? referenceType,
        string? referenceId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
