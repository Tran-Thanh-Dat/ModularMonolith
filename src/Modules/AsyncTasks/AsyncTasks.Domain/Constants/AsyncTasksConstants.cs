namespace AsyncTasks.Domain.Constants;

public static class AsyncTasksConstants
{
    public const string SchemaName = "async_tasks";
    public const string ProcessQueueName = "async-tasks.process";

    public const string FaultQueueName = "async-tasks.fault";
    public const int DefaultMaxRetryCount = 3;
    public const int MaxPayloadLength = 8000;
    public const int MaxErrorMessageLength = 2000;
}
