using Files.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Files.Infrastructure.Storage;

public sealed class FileStorageCompensationBuffer : IFileStorageCompensationBuffer
{
    private readonly IFileStorageProvider _storageProvider;
    private readonly ILogger<FileStorageCompensationBuffer> _logger;
    private readonly List<string> _pendingPaths = [];

    public FileStorageCompensationBuffer(
        IFileStorageProvider storageProvider,
        ILogger<FileStorageCompensationBuffer> logger)
    {
        _storageProvider = storageProvider;
        _logger = logger;
    }

    public void TrackPendingDeletion(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
        {
            return;
        }

        _pendingPaths.Add(storagePath);
    }

    public async Task CompensatePendingAsync(CancellationToken cancellationToken = default)
    {
        if (_pendingPaths.Count == 0)
        {
            return;
        }

        foreach (var storagePath in _pendingPaths.ToList())
        {
            try
            {
                await _storageProvider.DeleteAsync(storagePath, cancellationToken);
                _logger.LogInformation(
                    "Compensating delete removed pending upload at {StoragePath}",
                    storagePath);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to compensate delete for pending upload at {StoragePath}",
                    storagePath);
            }
        }

        _pendingPaths.Clear();
    }

    public void ClearPending() => _pendingPaths.Clear();
}
