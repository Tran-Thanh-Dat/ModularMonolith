using AuditLogs.Domain.AuditLogs;

namespace AuditLogs.Infrastructure.Persistence.Interceptors;

public interface IAuditChangeBuffer
{
    void SetPendingEntries(IReadOnlyCollection<AuditLog> entries);

    IReadOnlyCollection<AuditLog> TakePendingEntries();
}

public sealed class AuditChangeBuffer : IAuditChangeBuffer
{
    private IReadOnlyCollection<AuditLog> _entries = [];

    public void SetPendingEntries(IReadOnlyCollection<AuditLog> entries) =>
        _entries = entries;

    public IReadOnlyCollection<AuditLog> TakePendingEntries()
    {
        var entries = _entries;
        _entries = [];
        return entries;
    }
}
