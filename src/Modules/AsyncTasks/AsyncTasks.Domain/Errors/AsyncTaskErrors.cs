namespace AsyncTasks.Domain.Errors;

public static class AsyncTaskErrors
{
    public const string NotFound = "AsyncTask.NotFound";
    public const string TaskNoAlreadyExists = "AsyncTask.TaskNoAlreadyExists";
    public const string InvalidStatus = "AsyncTask.InvalidStatus";
    public const string InvalidType = "AsyncTask.InvalidType";
    public const string InvalidProgress = "AsyncTask.InvalidProgress";
    public const string AlreadyCompleted = "AsyncTask.AlreadyCompleted";
    public const string AlreadyFailed = "AsyncTask.AlreadyFailed";
    public const string AlreadyCancelled = "AsyncTask.AlreadyCancelled";
    public const string CannotCancel = "AsyncTask.CannotCancel";
    public const string CannotRetry = "AsyncTask.CannotRetry";
    public const string MaxRetryExceeded = "AsyncTask.MaxRetryExceeded";
    public const string MessageQueueDisabled = "AsyncTask.MessageQueueDisabled";
    public const string PublishFailed = "AsyncTask.PublishFailed";
    public const string ProcessorNotFound = "AsyncTask.ProcessorNotFound";
    public const string ProcessingFailed = "AsyncTask.ProcessingFailed";
    public const string PayloadTooLarge = "AsyncTask.PayloadTooLarge";
}
