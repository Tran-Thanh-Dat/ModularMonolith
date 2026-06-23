using AuditLogs.Application.AuditLogs.CreateAuditLog;

namespace AuditLogs.Application.Abstractions;

public interface IAuditLogService
{
    Task CreateAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
