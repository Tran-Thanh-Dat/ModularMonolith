namespace AuditLogs.Application.AuditLogs.GetAuditLogs;

public sealed class AuditLogListItemResponse
{
    public Guid Id { get; init; }

    public Guid? UserId { get; init; }

    public string? UserName { get; init; }

    public string Action { get; init; } = default!;

    public string? EntityName { get; init; }

    public string? EntityId { get; init; }

    public string ModuleName { get; init; } = default!;

    public string Status { get; init; } = default!;

    public DateTimeOffset CreatedAt { get; init; }
}
