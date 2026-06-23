namespace BackgroundJobs.Application.JobExecutions;

public sealed class BackgroundJobExecutionListItemResponse
{
    public Guid Id { get; init; }

    public string JobName { get; init; } = default!;

    public string JobType { get; init; } = default!;

    public string Status { get; init; } = default!;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public long? DurationMs { get; init; }

    public string? TriggeredBy { get; init; }

    public string TriggerSource { get; init; } = default!;

    public string? ResultMessage { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class BackgroundJobExecutionDetailResponse
{
    public Guid Id { get; init; }

    public string JobName { get; init; } = default!;

    public string JobType { get; init; } = default!;

    public string Status { get; init; } = default!;

    public DateTimeOffset StartedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public long? DurationMs { get; init; }

    public string? TriggeredBy { get; init; }

    public string TriggerSource { get; init; } = default!;

    public string? Parameters { get; init; }

    public string? ResultMessage { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public Guid? CreatedBy { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public Guid? UpdatedBy { get; init; }
}

public sealed class RunBackgroundJobResponse
{
    public Guid ExecutionId { get; init; }

    public string JobName { get; init; } = default!;

    public string Status { get; init; } = default!;

    public string? ResultMessage { get; init; }

    public string? ErrorMessage { get; init; }

    public int ProcessedCount { get; init; }

    public int SuccessCount { get; init; }

    public int FailedCount { get; init; }

    public int SkippedCount { get; init; }
}
