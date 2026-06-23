namespace Notifications.Application.Abstractions;

public sealed class EmailRetryBatchResult
{
    public int ProcessedCount { get; init; }

    public int SuccessCount { get; init; }

    public int FailedCount { get; init; }

    public int SkippedCount { get; init; }

    public string Summary =>
        $"Processed={ProcessedCount}, Success={SuccessCount}, Failed={FailedCount}, Skipped={SkippedCount}";
}

public interface IEmailRetryService
{
    Task<EmailRetryBatchResult> ProcessBatchAsync(
        int batchSize,
        int maxRetryCount,
        CancellationToken cancellationToken = default);
}
