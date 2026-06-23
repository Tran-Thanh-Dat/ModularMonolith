namespace AuditLogs.Application.Abstractions;

public interface IActivityLogBuffer
{
    void Enqueue(PendingActivityLogEntry entry);

    IReadOnlyCollection<PendingActivityLogEntry> TakeAll();

    void Clear();
}
