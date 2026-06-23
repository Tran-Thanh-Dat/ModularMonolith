using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using Notifications.Domain.Constants;

namespace Notifications.Domain.EmailMessages;

public sealed class EmailMessage : SoftDeletableEntity
{
    private EmailMessage()
    {
    }

    private EmailMessage(
        Guid id,
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        string? templateCode,
        string? templateData,
        string status,
        string? provider,
        string? referenceType,
        string? referenceId,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        To = to;
        Cc = cc;
        Bcc = bcc;
        Subject = subject;
        Body = body;
        IsHtml = isHtml;
        TemplateCode = templateCode;
        TemplateData = templateData;
        Status = status;
        Provider = provider;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        RetryCount = 0;
        SetCreated(createdBy, createdAt);
    }

    public string To { get; private set; } = default!;

    public string? Cc { get; private set; }

    public string? Bcc { get; private set; }

    public string Subject { get; private set; } = default!;

    public string Body { get; private set; } = default!;

    public bool IsHtml { get; private set; }

    public string? TemplateCode { get; private set; }

    public string? TemplateData { get; private set; }

    public string Status { get; private set; } = default!;

    public string? ErrorMessage { get; private set; }

    public DateTimeOffset? SentAt { get; private set; }

    public int RetryCount { get; private set; }

    public DateTimeOffset? NextRetryAt { get; private set; }

    public string? Provider { get; private set; }

    public string? ReferenceType { get; private set; }

    public string? ReferenceId { get; private set; }

    public static EmailMessage CreatePending(
        string to,
        string? cc,
        string? bcc,
        string subject,
        string body,
        bool isHtml,
        string? templateCode,
        string? templateData,
        string? provider,
        string? referenceType,
        string? referenceId,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(to))
        {
            throw new DomainException("Email recipient is required.", "Email.InvalidRecipient");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new DomainException("Email subject is required.", "Email.SendFailed");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new DomainException("Email body is required.", "Email.SendFailed");
        }

        return new EmailMessage(
            Guid.NewGuid(),
            to.Trim(),
            string.IsNullOrWhiteSpace(cc) ? null : cc.Trim(),
            string.IsNullOrWhiteSpace(bcc) ? null : bcc.Trim(),
            subject.Trim(),
            body,
            isHtml,
            string.IsNullOrWhiteSpace(templateCode) ? null : templateCode.Trim(),
            templateData,
            EmailStatuses.Pending,
            provider,
            string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim(),
            string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim(),
            createdAt,
            createdBy);
    }

    public void MarkSent(string provider, DateTimeOffset sentAt)
    {
        Status = EmailStatuses.Sent;
        Provider = provider;
        SentAt = sentAt;
        ErrorMessage = null;
    }

    public void MarkFailed(string? errorMessage)
    {
        Status = EmailStatuses.Failed;
        ErrorMessage = errorMessage;
    }

    public void ScheduleRetry(DateTimeOffset nextRetryAt)
    {
        RetryCount++;
        NextRetryAt = nextRetryAt;
        Status = EmailStatuses.Pending;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Email message is already deleted.", "Email.MessageNotFound");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
