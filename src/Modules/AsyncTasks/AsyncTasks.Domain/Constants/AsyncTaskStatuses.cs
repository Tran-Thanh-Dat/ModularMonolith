namespace AsyncTasks.Domain.Constants;

public static class AsyncTaskStatuses
{
    public const string Pending = "PENDING";
    public const string Queued = "QUEUED";
    public const string Processing = "PROCESSING";
    public const string Completed = "COMPLETED";
    public const string Failed = "FAILED";
    public const string Cancelled = "CANCELLED";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending, Queued, Processing, Completed, Failed, Cancelled
    };

    public static readonly IReadOnlySet<string> Terminal = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Completed, Failed, Cancelled
    };

    public static readonly IReadOnlySet<string> Cancellable = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Pending, Queued, Processing
    };
}
