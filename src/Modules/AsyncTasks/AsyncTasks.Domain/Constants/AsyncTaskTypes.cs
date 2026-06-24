namespace AsyncTasks.Domain.Constants;

public static class AsyncTaskTypes
{
    public const string EmailDemo = "EMAIL_DEMO";
    public const string FileProcessingDemo = "FILE_PROCESSING_DEMO";
    public const string FailDemo = "FAIL_DEMO";
    public const string LongRunningDemo = "LONG_RUNNING_DEMO";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        EmailDemo, FileProcessingDemo, FailDemo, LongRunningDemo
    };
}
