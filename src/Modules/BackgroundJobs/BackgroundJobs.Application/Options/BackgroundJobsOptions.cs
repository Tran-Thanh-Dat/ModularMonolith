namespace BackgroundJobs.Application.Options;

public sealed class BackgroundJobsOptions
{
    public const string SectionName = "BackgroundJobs";

    public bool Enabled { get; set; } = true;

    public DashboardOptions Dashboard { get; set; } = new();

    public ScheduleOptions Schedules { get; set; } = new();

    public EmailRetryOptions EmailRetry { get; set; } = new();

    public TemporaryFilesOptions TemporaryFiles { get; set; } = new();

    public LogCleanupOptions LogCleanup { get; set; } = new();
}

public sealed class DashboardOptions
{
    public bool Enabled { get; set; } = true;

    public string Path { get; set; } = "/hangfire";

    public DashboardBasicAuthOptions BasicAuth { get; set; } = new();
}

public sealed class DashboardBasicAuthOptions
{
    public bool Enabled { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class ScheduleOptions
{
    public string EmailRetryCron { get; set; } = "*/5 * * * *";

    public string TemporaryFileCleanupCron { get; set; } = "0 * * * *";

    public string LogCleanupCron { get; set; } = "0 2 * * *";
}

public sealed class EmailRetryOptions
{
    public int BatchSize { get; set; } = 50;

    public int MaxRetryCount { get; set; } = 5;
}

public sealed class TemporaryFilesOptions
{
    public int BatchSize { get; set; } = 100;

    public bool DeletePhysicalFiles { get; set; } = true;
}

public sealed class LogCleanupOptions
{
    public bool Enabled { get; set; }

    public int RetentionDays { get; set; } = 90;
}
