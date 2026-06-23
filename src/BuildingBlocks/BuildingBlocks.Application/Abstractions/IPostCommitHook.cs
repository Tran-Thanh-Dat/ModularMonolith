namespace BuildingBlocks.Application.Abstractions;

/// <summary>
/// Runs after a transactional command successfully commits, or clears pending work on rollback.
/// </summary>
public interface IPostCommitHook
{
    Task OnCommittedAsync(CancellationToken cancellationToken = default);

    Task OnRollbackAsync(CancellationToken cancellationToken = default);
}
