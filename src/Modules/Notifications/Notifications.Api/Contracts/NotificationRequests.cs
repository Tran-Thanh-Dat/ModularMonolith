namespace Notifications.Api.Contracts;

public sealed class SendTestEmailRequest
{
    public string To { get; init; } = default!;

    public string Subject { get; init; } = default!;

    public string Body { get; init; } = default!;
}

public sealed class SendEmailRequestDto
{
    public string To { get; init; } = default!;

    public string? Cc { get; init; }

    public string? Bcc { get; init; }

    public string Subject { get; init; } = default!;

    public string Body { get; init; } = default!;

    public bool IsHtml { get; init; } = true;

    public string? ReferenceType { get; init; }

    public string? ReferenceId { get; init; }
}

public sealed class SendTemplateEmailRequest
{
    public string TemplateCode { get; init; } = default!;

    public string To { get; init; } = default!;

    public string? Cc { get; init; }

    public string? Bcc { get; init; }

    public Dictionary<string, string> TemplateData { get; init; } = new();

    public string? ReferenceType { get; init; }

    public string? ReferenceId { get; init; }
}

public sealed class CreateEmailTemplateRequest
{
    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string Subject { get; init; } = default!;

    public string Body { get; init; } = default!;

    public bool IsHtml { get; init; } = true;

    public string? Description { get; init; }
}

public sealed class UpdateEmailTemplateRequest
{
    public string Name { get; init; } = default!;

    public string Subject { get; init; } = default!;

    public string Body { get; init; } = default!;

    public bool IsHtml { get; init; } = true;

    public string? Description { get; init; }
}

public sealed class CreateNotificationRequest
{
    public Guid UserId { get; init; }

    public string Title { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string Type { get; init; } = default!;

    public string? ReferenceType { get; init; }

    public string? ReferenceId { get; init; }

    public string? Metadata { get; init; }
}
