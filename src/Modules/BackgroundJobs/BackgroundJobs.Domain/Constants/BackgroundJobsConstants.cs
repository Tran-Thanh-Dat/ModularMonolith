namespace BackgroundJobs.Domain.Constants;

public static class BackgroundJobsConstants
{
    public const string SchemaName = "background_jobs";
}

public static class BackgroundJobStatuses
{
    public const string Running = "Running";
    public const string Succeeded = "Succeeded";
    public const string Failed = "Failed";
    public const string Skipped = "Skipped";
}

public static class BackgroundJobTypes
{
    public const string EmailRetry = "EmailRetry";
    public const string TemporaryFileCleanup = "TemporaryFileCleanup";
    public const string PhysicalFilePurge = "PhysicalFilePurge";
    public const string AuditLogCleanup = "AuditLogCleanup";
    public const string ActivityLogCleanup = "ActivityLogCleanup";
    public const string Manual = "Manual";
}

public static class BackgroundJobTriggerSources
{
    public const string Recurring = "Recurring";
    public const string Manual = "Manual";
    public const string System = "System";
}

public static class RecurringJobIds
{
    public const string EmailRetry = "background-jobs:email-retry";
    public const string TemporaryFileCleanup = "background-jobs:temporary-file-cleanup";
    public const string LogCleanup = "background-jobs:log-cleanup";
}
