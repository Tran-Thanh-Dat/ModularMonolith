using AsyncTasks.Application.Dtos;

namespace AsyncTasks.Application.Abstractions;

public interface IAsyncTaskService
{
    Task<AsyncTaskDto> SubmitEmailDemoAsync(
        string? emailTo,
        string? subject,
        string? body,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<AsyncTaskDto> SubmitFileProcessingDemoAsync(
        Guid? fileId,
        string? fileName,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<AsyncTaskDto> SubmitFailDemoAsync(
        string? failReason,
        bool shouldAlwaysFail,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<AsyncTaskDto> SubmitLongRunningDemoAsync(
        int durationSeconds,
        int steps,
        Guid? tenantId,
        Guid? organizationId,
        CancellationToken cancellationToken = default);

    Task<AsyncTaskDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<BuildingBlocks.Application.Pagination.PagedResult<AsyncTaskDto>> GetPagedAsync(
        AsyncTaskListFilter filter,
        CancellationToken cancellationToken = default);

    Task CancelAsync(Guid id, CancellationToken cancellationToken = default);

    Task<AsyncTaskDto> RetryAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkQueuedAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkProcessingAsync(Guid id, string consumerName, CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(Guid id, string? result, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(Guid id, string errorCode, string errorMessage, CancellationToken cancellationToken = default);

    Task UpdateProgressAsync(Guid id, int progressPercent, CancellationToken cancellationToken = default);
}

public sealed class AsyncTaskListFilter
{
    public string? Keyword { get; init; }

    public string? TaskNo { get; init; }

    public string? Type { get; init; }

    public string? Status { get; init; }

    public Guid? RequestedByUserId { get; init; }

    public Guid? TenantId { get; init; }

    public Guid? OrganizationId { get; init; }

    public DateTimeOffset? CreatedFrom { get; init; }

    public DateTimeOffset? CreatedTo { get; init; }

    public int PageIndex { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}
