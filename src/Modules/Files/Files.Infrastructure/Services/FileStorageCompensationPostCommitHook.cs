using BuildingBlocks.Application.Abstractions;
using Files.Application.Abstractions;

namespace Files.Infrastructure.Services;

/// <summary>
/// Clears pending upload compensation on successful commit, or deletes orphaned
/// physical files when the command transaction rolls back.
/// </summary>
public sealed class FileStorageCompensationPostCommitHook : IPostCommitHook
{
    private readonly IFileStorageCompensationBuffer _compensationBuffer;

    public FileStorageCompensationPostCommitHook(IFileStorageCompensationBuffer compensationBuffer)
    {
        _compensationBuffer = compensationBuffer;
    }

    public Task OnCommittedAsync(CancellationToken cancellationToken = default)
    {
        _compensationBuffer.ClearPending();
        return Task.CompletedTask;
    }

    public Task OnRollbackAsync(CancellationToken cancellationToken = default) =>
        _compensationBuffer.CompensatePendingAsync(cancellationToken);
}
