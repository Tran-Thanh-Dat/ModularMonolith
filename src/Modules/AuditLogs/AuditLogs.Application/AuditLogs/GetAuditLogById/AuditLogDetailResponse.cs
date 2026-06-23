namespace AuditLogs.Application.AuditLogs.GetAuditLogById;

public sealed class AuditLogDetailResponse
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string Action { get; init; } = default!;

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public string ModuleName { get; init; } = default!;

    public string? OldValues { get; init; }

    public string? NewValues { get; init; }

    public string? ChangedColumns { get; init; }

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string Status { get; init; } = default!;

    public string? ErrorMessage { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
