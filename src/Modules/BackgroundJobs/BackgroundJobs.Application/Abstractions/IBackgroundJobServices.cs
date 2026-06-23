using BackgroundJobs.Application.JobExecutions;
using BuildingBlocks.Application.Pagination;

namespace BackgroundJobs.Application.Abstractions;

public interface IBackgroundJobExecutionService
{
    Task<Guid> StartAsync(
        string jobName,
        string jobType,
        string? triggeredBy,
        string triggerSource,
        string? parameters,
        CancellationToken cancellationToken = default);

    Task MarkSucceededAsync(
        Guid executionId,
        string? resultMessage,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        Guid executionId,
        string? errorMessage,
        string? errorDetails,
        CancellationToken cancellationToken = default);

    Task MarkSkippedAsync(
        Guid executionId,
        string? resultMessage,
        CancellationToken cancellationToken = default);

    Task<BackgroundJobExecutionDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BackgroundJobExecutionListItemResponse>> GetListAsync(
        string? keyword,
        string? jobName,
        string? jobType,
        string? status,
        string? triggerSource,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}

public interface IBackgroundJobRunner
{
    Task<RunBackgroundJobResponse> RunEmailRetryAsync(
        int? batchSize,
        string? triggeredBy,
        CancellationToken cancellationToken = default);

    Task<RunBackgroundJobResponse> RunTemporaryFileCleanupAsync(
        int? batchSize,
        string? triggeredBy = null,
        CancellationToken cancellationToken = default);

    Task<RunBackgroundJobResponse> RunLogCleanupAsync(
        string? triggeredBy = null,
        CancellationToken cancellationToken = default);
}

public sealed class LogCleanupBatchResult
{
    public int ProcessedCount { get; init; }

    public int SkippedCount { get; init; }

    public string Summary { get; init; } = default!;

    public bool WasSkipped { get; init; }
}

public interface ILogCleanupService
{
    Task<LogCleanupBatchResult> ProcessAsync(
        bool enabled,
        int retentionDays,
        CancellationToken cancellationToken = default);
}
