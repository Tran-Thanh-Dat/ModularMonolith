namespace Notifications.Infrastructure.Caching;

internal sealed class CachedEmailTemplate
{
    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public bool IsHtml { get; init; }

    public bool IsActive { get; init; }
}
