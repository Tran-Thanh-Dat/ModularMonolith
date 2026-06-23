namespace Files.Application.Abstractions;

public sealed class TemporaryFileCleanupBatchResult
{
    public int ProcessedCount { get; init; }

    public int SuccessCount { get; init; }

    public int FailedCount { get; init; }

    public int SkippedCount { get; init; }

    public string Summary =>
        $"Processed={ProcessedCount}, Success={SuccessCount}, Failed={FailedCount}, Skipped={SkippedCount}";
}

public interface ITemporaryFileCleanupService
{
    Task<TemporaryFileCleanupBatchResult> ProcessBatchAsync(
        int batchSize,
        bool deletePhysicalFiles,
        CancellationToken cancellationToken = default);
}
