namespace Notifications.Application.EmailMessages;



public sealed class EmailMessageListItemResponse

{

    public Guid Id { get; init; }



    public string To { get; init; } = default!;



    public string Subject { get; init; } = default!;



    public string Status { get; init; } = default!;



    public string? TemplateCode { get; init; }



    public string? Provider { get; init; }



    public DateTimeOffset? SentAt { get; init; }



    public DateTimeOffset CreatedAt { get; init; }

}



public sealed class EmailMessageDetailResponse

{

    public Guid Id { get; init; }



    public string To { get; init; } = default!;



    public string? Cc { get; init; }



    public string? Bcc { get; init; }



    public string Subject { get; init; } = default!;



    public string Body { get; init; } = default!;



    public bool IsHtml { get; init; }



    public string Status { get; init; } = default!;



    public string? TemplateCode { get; init; }



    public string? TemplateData { get; init; }



    public string? Provider { get; init; }



    public string? ErrorMessage { get; init; }



    public DateTimeOffset? SentAt { get; init; }



    public int RetryCount { get; init; }



    public DateTimeOffset? NextRetryAt { get; init; }



    public string? ReferenceType { get; init; }



    public string? ReferenceId { get; init; }



    public DateTimeOffset CreatedAt { get; init; }



    public Guid? CreatedBy { get; init; }



    public DateTimeOffset? UpdatedAt { get; init; }



    public Guid? UpdatedBy { get; init; }

}



public sealed class SendEmailResponse

{

    public Guid EmailMessageId { get; init; }



    public bool IsSuccess { get; init; }



    public string Status { get; init; } = default!;



    public string? ErrorMessage { get; init; }



    public DateTimeOffset? SentAt { get; init; }

}


