namespace AuditLogs.Application.ActivityLogs.GetActivityLogs;

public sealed class ActivityLogListItemResponse
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string ActivityType { get; init; } = default!;

    public string Description { get; init; } = default!;

    public string ModuleName { get; init; } = default!;

    public string Status { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
}
