using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using Notifications.Domain.Constants;

namespace Notifications.Domain.Notifications;

public sealed class Notification : SoftDeletableEntity
{
    private Notification()
    {
    }

    private Notification(
        Guid id,
        Guid userId,
        string title,
        string message,
        string type,
        string? referenceType,
        string? referenceId,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        Status = NotificationStatuses.New;
        IsRead = false;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        Metadata = metadata;
        SetCreated(createdBy, createdAt);
    }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = default!;

    public string Message { get; private set; } = default!;

    public string Type { get; private set; } = default!;

    public string Status { get; private set; } = default!;

    public bool IsRead { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public string? ReferenceType { get; private set; }

    public string? ReferenceId { get; private set; }

    public string? Metadata { get; private set; }

    public static Notification Create(
        Guid userId,
        string title,
        string message,
        string type,
        string? referenceType,
        string? referenceId,
        string? metadata,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("Notification user is required.", "Notification.UserRequired");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Notification title is required.", "Notification.NotFound");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("Notification message is required.", "Notification.NotFound");
        }

        return new Notification(
            Guid.NewGuid(),
            userId,
            title.Trim(),
            message.Trim(),
            type,
            string.IsNullOrWhiteSpace(referenceType) ? null : referenceType.Trim(),
            string.IsNullOrWhiteSpace(referenceId) ? null : referenceId.Trim(),
            metadata,
            createdAt,
            createdBy);
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (IsRead && Status == NotificationStatuses.Read)
        {
            return;
        }

        IsRead = true;
        ReadAt = readAt;
        Status = NotificationStatuses.Read;
    }

    public void Archive()
    {
        if (Status == NotificationStatuses.Archived)
        {
            return;
        }

        Status = NotificationStatuses.Archived;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Notification is already deleted.", "Notification.NotFound");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
