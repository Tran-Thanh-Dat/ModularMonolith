namespace AuditLogs.Application.AuditLogs.CreateAuditLog;

public sealed class AuditLogEntry
{
    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string ModuleName { get; init; } = default!;

    public string Action { get; init; } = default!;

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public object? OldValues { get; init; }

    public object? NewValues { get; init; }

    public object? ChangedColumns { get; init; }

    public string? RequestPath { get; init; }

    public string? HttpMethod { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string Status { get; init; } = "Success";

    public string? ErrorMessage { get; init; }
}
