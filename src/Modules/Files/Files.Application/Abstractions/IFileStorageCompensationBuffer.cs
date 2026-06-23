namespace Files.Application.Abstractions;

/// <summary>
/// Tracks physical files written during the current request so they can be
/// deleted if the surrounding command transaction rolls back.
/// </summary>
public interface IFileStorageCompensationBuffer
{
    void TrackPendingDeletion(string storagePath);

    Task CompensatePendingAsync(CancellationToken cancellationToken = default);

    void ClearPending();
}
