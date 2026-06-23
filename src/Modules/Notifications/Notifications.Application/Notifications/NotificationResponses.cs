namespace Notifications.Application.Notifications;



public sealed class NotificationListItemResponse

{

    public Guid Id { get; init; }



    public Guid UserId { get; init; }



    public string Title { get; init; } = default!;



    public string Message { get; init; } = default!;



    public string Type { get; init; } = default!;



    public string Status { get; init; } = default!;



    public bool IsRead { get; init; }



    public DateTimeOffset? ReadAt { get; init; }



    public string? ReferenceType { get; init; }



    public string? ReferenceId { get; init; }



    public DateTimeOffset CreatedAt { get; init; }

}



public sealed class NotificationDetailResponse

{

    public Guid Id { get; init; }



    public Guid UserId { get; init; }



    public string Title { get; init; } = default!;



    public string Message { get; init; } = default!;



    public string Type { get; init; } = default!;



    public string Status { get; init; } = default!;



    public bool IsRead { get; init; }



    public DateTimeOffset? ReadAt { get; init; }



    public string? ReferenceType { get; init; }



    public string? ReferenceId { get; init; }



    public string? Metadata { get; init; }



    public DateTimeOffset CreatedAt { get; init; }



    public Guid? CreatedBy { get; init; }



    public DateTimeOffset? UpdatedAt { get; init; }



    public Guid? UpdatedBy { get; init; }

}



public sealed class CreateNotificationResponse

{

    public Guid Id { get; init; }

}


