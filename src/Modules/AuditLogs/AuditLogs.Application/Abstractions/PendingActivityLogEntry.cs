namespace AuditLogs.Application.Abstractions;

public sealed record PendingActivityLogEntry(
    Guid? UserId,
    string? UserName,
    string ActivityType,
    string Description,
    string ModuleName,
    string Status,
    string? ErrorMessage);
