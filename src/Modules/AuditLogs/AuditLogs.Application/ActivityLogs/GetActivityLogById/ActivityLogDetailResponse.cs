namespace AuditLogs.Application.ActivityLogs.GetActivityLogById;

public sealed class ActivityLogDetailResponse
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string ActivityType { get; init; } = default!;

    public string Description { get; init; } = default!;

    public string ModuleName { get; init; } = default!;

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string Status { get; init; } = default!;

    public string? ErrorMessage { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
