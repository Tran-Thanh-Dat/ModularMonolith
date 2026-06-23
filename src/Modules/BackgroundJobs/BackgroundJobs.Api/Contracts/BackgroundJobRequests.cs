namespace BackgroundJobs.Api.Contracts;

public sealed class RunEmailRetryJobRequest
{
    public int? BatchSize { get; init; }
}

public sealed class RunTemporaryFileCleanupJobRequest
{
    public int? BatchSize { get; init; }
}

public sealed class RunLogCleanupJobRequest
{
}
