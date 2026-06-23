namespace Notifications.Application.Abstractions;

public sealed class SendEmailRequest
{
    public string To { get; init; } = default!;

    public string? Cc { get; init; }

    public string? Bcc { get; init; }

    public string Subject { get; init; } = default!;

    public string Body { get; init; } = default!;

    public bool IsHtml { get; init; } = true;

    public string? OriginalTo { get; init; }

    public string? OriginalCc { get; init; }

    public string? OriginalBcc { get; init; }
}

public sealed class SendEmailResult
{
    public bool IsSuccess { get; init; }

    public string Provider { get; init; } = default!;

    public string? ErrorMessage { get; init; }

    public DateTimeOffset? SentAt { get; init; }
}

public interface IEmailSender
{
    Task<SendEmailResult> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default);
}
