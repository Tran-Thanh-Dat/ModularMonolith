namespace AsyncTasks.Application.Constants;

public static class AsyncTaskActivityTypes
{
    public const string Submitted = "AsyncTaskSubmitted";
    public const string Queued = "AsyncTaskQueued";
    public const string Started = "AsyncTaskStarted";
    public const string Completed = "AsyncTaskCompleted";
    public const string Failed = "AsyncTaskFailed";
    public const string Cancelled = "AsyncTaskCancelled";
    public const string Retried = "AsyncTaskRetried";
}
