using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using Files.Application.Abstractions;
using Files.Domain.FileResources;
using Files.Infrastructure.Persistence;
using Files.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Files.Infrastructure.Services;

public sealed class TemporaryFileCleanupService : ITemporaryFileCleanupService
{
    private readonly FilesUnitOfWork _unitOfWork;
    private readonly IFileStorageProvider _storageProvider;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ILogger<TemporaryFileCleanupService> _logger;

    public TemporaryFileCleanupService(
        FilesUnitOfWork unitOfWork,
        IFileStorageProvider storageProvider,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ILogger<TemporaryFileCleanupService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageProvider = storageProvider;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _logger = logger;
    }

    public async Task<TemporaryFileCleanupBatchResult> ProcessBatchAsync(
        int batchSize,
        bool deletePhysicalFiles,
        CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var files = await _unitOfWork.Repository<FileResource, Guid>()
            .Query()
            .Where(file =>
                !file.IsDeleted &&
                file.IsTemporary &&
                file.ExpiresAt != null &&
                file.ExpiresAt <= now)
            .OrderBy(file => file.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        var successCount = 0;
        var failedCount = 0;

        foreach (var file in files)
        {
            if (!file.IsTemporary)
            {
                continue;
            }

            try
            {
                if (deletePhysicalFiles)
                {
                    try
                    {
                        await _storageProvider.DeleteAsync(file.StoragePath, cancellationToken);
                    }
                    catch (Exception exception)
                    {
                        _logger.LogWarning(
                            exception,
                            "Physical delete failed for temporary file {FileId}",
                            file.Id);
                        failedCount++;
                        continue;
                    }
                }

                file.SoftDelete(null, now);
                successCount++;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Temporary file cleanup failed for {FileId}", file.Id);
                failedCount++;

                await _activityLogService.LogImmediateAsync(
                    ActivityTypes.SystemError,
                    $"Temporary file cleanup failed: {file.OriginalFileName}",
                    AuditLogConstants.Modules.Files,
                    "Failed",
                    "Temporary file cleanup failed.",
                    cancellationToken: cancellationToken);
            }
        }

        return new TemporaryFileCleanupBatchResult
        {
            ProcessedCount = files.Count,
            SuccessCount = successCount,
            FailedCount = failedCount,
            SkippedCount = 0
        };
    }
}
