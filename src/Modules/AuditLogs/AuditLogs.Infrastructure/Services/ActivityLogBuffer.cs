using AuditLogs.Application.Abstractions;

namespace AuditLogs.Infrastructure.Services;

public sealed class ActivityLogBuffer : IActivityLogBuffer
{
    private readonly List<PendingActivityLogEntry> _entries = [];

    public void Enqueue(PendingActivityLogEntry entry) =>
        _entries.Add(entry);

    public IReadOnlyCollection<PendingActivityLogEntry> TakeAll()
    {
        if (_entries.Count == 0)
        {
            return [];
        }

        var entries = _entries.ToArray();
        _entries.Clear();
        return entries;
    }

    public void Clear() =>
        _entries.Clear();
}
