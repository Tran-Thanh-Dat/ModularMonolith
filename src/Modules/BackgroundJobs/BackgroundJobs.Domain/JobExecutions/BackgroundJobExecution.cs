using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;
using BackgroundJobs.Domain.Constants;

namespace BackgroundJobs.Domain.JobExecutions;

public sealed class BackgroundJobExecution : SoftDeletableEntity
{
    private BackgroundJobExecution()
    {
    }

    private BackgroundJobExecution(
        Guid id,
        string jobName,
        string jobType,
        string status,
        DateTimeOffset startedAt,
        string? triggeredBy,
        string triggerSource,
        string? parameters,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        JobName = jobName;
        JobType = jobType;
        Status = status;
        StartedAt = startedAt;
        TriggeredBy = triggeredBy;
        TriggerSource = triggerSource;
        Parameters = parameters;
        SetCreated(createdBy, createdAt);
    }

    public string JobName { get; private set; } = default!;

    public string JobType { get; private set; } = default!;

    public string Status { get; private set; } = default!;

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public long? DurationMs { get; private set; }

    public string? TriggeredBy { get; private set; }

    public string TriggerSource { get; private set; } = default!;

    public string? Parameters { get; private set; }

    public string? ResultMessage { get; private set; }

    public string? ErrorMessage { get; private set; }

    public string? ErrorDetails { get; private set; }

    public static BackgroundJobExecution Start(
        string jobName,
        string jobType,
        string? triggeredBy,
        string triggerSource,
        string? parameters,
        DateTimeOffset startedAt,
        Guid? createdBy = null) =>
        new(
            Guid.NewGuid(),
            jobName.Trim(),
            jobType.Trim(),
            BackgroundJobStatuses.Running,
            startedAt,
            string.IsNullOrWhiteSpace(triggeredBy) ? null : triggeredBy.Trim(),
            triggerSource.Trim(),
            parameters,
            startedAt,
            createdBy);

    public void MarkSucceeded(string? resultMessage, DateTimeOffset finishedAt)
    {
        Status = BackgroundJobStatuses.Succeeded;
        FinishedAt = finishedAt;
        DurationMs = (long)(finishedAt - StartedAt).TotalMilliseconds;
        ResultMessage = resultMessage;
        ErrorMessage = null;
        ErrorDetails = null;
    }

    public void MarkFailed(string? errorMessage, string? errorDetails, DateTimeOffset finishedAt)
    {
        Status = BackgroundJobStatuses.Failed;
        FinishedAt = finishedAt;
        DurationMs = (long)(finishedAt - StartedAt).TotalMilliseconds;
        ErrorMessage = errorMessage;
        ErrorDetails = errorDetails;
    }

    public void MarkSkipped(string? resultMessage, DateTimeOffset finishedAt)
    {
        Status = BackgroundJobStatuses.Skipped;
        FinishedAt = finishedAt;
        DurationMs = (long)(finishedAt - StartedAt).TotalMilliseconds;
        ResultMessage = resultMessage;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Job execution is already deleted.", "BackgroundJob.NotFound");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
