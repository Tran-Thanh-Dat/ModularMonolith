using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Files.Application.Abstractions;
using Files.Application.Files;
using Files.Application.Options;
using Files.Application.Validation;
using Files.Domain.FileResources;
using Files.Infrastructure.Persistence;
using Files.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Files.Infrastructure.Services;

public sealed class FileService : IFileService
{
    private readonly FilesUnitOfWork _unitOfWork;
    private readonly IFileStorageProvider _storageProvider;
    private readonly IFileStorageCompensationBuffer _compensationBuffer;
    private readonly FileStorageOptions _fileStorageOptions;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<FileService> _logger;

    public FileService(
        FilesUnitOfWork unitOfWork,
        IFileStorageProvider storageProvider,
        IFileStorageCompensationBuffer compensationBuffer,
        IOptions<FileStorageOptions> fileStorageOptions,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<FileService> logger)
    {
        _unitOfWork = unitOfWork;
        _storageProvider = storageProvider;
        _compensationBuffer = compensationBuffer;
        _fileStorageOptions = fileStorageOptions.Value;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheService = cacheService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<UploadFileResponse> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string? moduleName,
        string? referenceType,
        string? referenceId,
        string? description,
        bool isTemporary,
        CancellationToken cancellationToken = default)
    {
        var sanitizedOriginalFileName = FileNameSanitizer.SanitizeOriginalFileName(originalFileName);
        var extension = FileValidationRules.GetExtensionFromFileName(sanitizedOriginalFileName);
        var folder = _dateTimeProvider.UtcNow.ToString("yyyy/MM/dd");

        StoredFileResult storedFile;
        try
        {
            storedFile = await _storageProvider.SaveAsync(
                fileStream,
                sanitizedOriginalFileName,
                contentType,
                folder,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to store uploaded file {OriginalFileName}",
                sanitizedOriginalFileName);

            throw new BadRequestException(
                FileErrors.UploadFailed,
                "File upload failed.");
        }

        _compensationBuffer.TrackPendingDeletion(storedFile.StoragePath);

        ValidateActualFileSize(storedFile.SizeInBytes);

        await ValidateFileSignatureAsync(storedFile.StoragePath, extension, cancellationToken);

        var fileResource = FileResource.Create(
            sanitizedOriginalFileName,
            storedFile.StoredFileName,
            extension,
            storedFile.ContentType,
            storedFile.SizeInBytes,
            _storageProvider.ProviderName,
            storedFile.StoragePath,
            storedFile.PublicUrl,
            storedFile.Checksum,
            moduleName,
            referenceType,
            referenceId,
            description,
            isTemporary,
            expiresAt: null,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<FileResource, Guid>().AddAsync(fileResource, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Uploaded file: {fileResource.OriginalFileName}",
            AuditLogConstants.Modules.Files,
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Registered file metadata {FileId} for {OriginalFileName}",
            fileResource.Id,
            fileResource.OriginalFileName);

        return new UploadFileResponse
        {
            Id = fileResource.Id,
            OriginalFileName = fileResource.OriginalFileName,
            ContentType = fileResource.ContentType,
            SizeInBytes = fileResource.SizeInBytes,
            PublicUrl = fileResource.PublicUrl
        };
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileResource = await GetFileForUpdateAsync(id, cancellationToken);
        fileResource.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted file: {fileResource.OriginalFileName}",
            AuditLogConstants.Modules.Files,
            cancellationToken: cancellationToken);

        InvalidateFileResourceCache(id);

        _logger.LogInformation("Soft-deleted file metadata {FileId}", id);
    }

    public async Task MarkAsPermanentAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fileResource = await GetFileForUpdateAsync(id, cancellationToken);

        if (!fileResource.IsTemporary)
        {
            return;
        }

        fileResource.MarkAsPermanent();

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Marked file as permanent: {fileResource.OriginalFileName}",
            AuditLogConstants.Modules.Files,
            cancellationToken: cancellationToken);

        InvalidateFileResourceCache(id);

        _logger.LogInformation("Marked file {FileId} as permanent", id);
    }

    public async Task<FileResourceDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.FileResourceDetail(id);
        var cached = await _cacheService.GetAsync<FileResourceDetailResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var fileResource = await _unitOfWork.Repository<FileResource, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(file => file.Id == id && !file.IsDeleted, cancellationToken);

        if (fileResource is null)
        {
            return null;
        }

        var detail = MapDetail(fileResource);
        await _cacheService.SetAsync(cacheKey, detail, cancellationToken: cancellationToken);
        return detail;
    }

    public async Task<PagedResult<FileResourceListItemResponse>> GetListAsync(
        string? keyword,
        string? moduleName,
        string? referenceType,
        string? referenceId,
        string? contentType,
        string? fileExtension,
        bool? isTemporary,
        bool? isActive,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<FileResource, Guid>()
            .QueryReadOnly()
            .Where(file => !file.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(file =>
                EF.Functions.ILike(file.OriginalFileName, pattern) ||
                EF.Functions.ILike(file.StoredFileName, pattern) ||
                EF.Functions.ILike(file.Description ?? string.Empty, pattern) ||
                EF.Functions.ILike(file.ModuleName ?? string.Empty, pattern) ||
                EF.Functions.ILike(file.ReferenceType ?? string.Empty, pattern) ||
                EF.Functions.ILike(file.ReferenceId ?? string.Empty, pattern));
        }

        if (!string.IsNullOrWhiteSpace(moduleName))
        {
            query = query.Where(file => file.ModuleName == moduleName.Trim());
        }

        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            query = query.Where(file => file.ReferenceType == referenceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            query = query.Where(file => file.ReferenceId == referenceId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(contentType))
        {
            query = query.Where(file => file.ContentType == contentType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(fileExtension))
        {
            var normalizedExtension = FileValidationRules.NormalizeExtension(fileExtension);
            query = query.Where(file => file.FileExtension == normalizedExtension);
        }

        if (isTemporary.HasValue)
        {
            query = query.Where(file => file.IsTemporary == isTemporary.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(file => file.IsActive == isActive.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(file => file.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(file => file.CreatedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(file => file.CreatedAt)
            .Select(file => new FileResourceListItemResponse
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                FileExtension = file.FileExtension,
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                StorageProvider = file.StorageProvider,
                PublicUrl = file.PublicUrl,
                ModuleName = file.ModuleName,
                ReferenceType = file.ReferenceType,
                ReferenceId = file.ReferenceId,
                Description = file.Description,
                IsTemporary = file.IsTemporary,
                ExpiresAt = file.ExpiresAt,
                IsActive = file.IsActive,
                CreatedAt = file.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<FileDownloadResult> DownloadAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var fileResource = await _unitOfWork.Repository<FileResource, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(file => file.Id == id && !file.IsDeleted, cancellationToken);

        if (fileResource is null)
        {
            throw new NotFoundException(
                FileErrors.NotFound,
                $"File with id '{id}' was not found.");
        }

        try
        {
            if (!await _storageProvider.ExistsAsync(fileResource.StoragePath, cancellationToken))
            {
                throw new NotFoundException(
                    FileErrors.NotFound,
                    $"Physical file for id '{id}' was not found.");
            }

            var stream = await _storageProvider.OpenReadAsync(fileResource.StoragePath, cancellationToken);

            return new FileDownloadResult
            {
                Content = stream,
                ContentType = fileResource.ContentType,
                FileName = fileResource.OriginalFileName
            };
        }
        catch (NotFoundException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to download file {FileId}",
                id);

            throw new BadRequestException(
                FileErrors.DownloadFailed,
                "File download failed.");
        }
    }

    private void ValidateActualFileSize(long actualSizeInBytes)
    {
        if (actualSizeInBytes <= 0)
        {
            throw new BadRequestException(
                FileErrors.Empty,
                "Uploaded file is empty.");
        }

        var maxBytes = _fileStorageOptions.MaxFileSizeMb * 1024L * 1024L;
        if (actualSizeInBytes > maxBytes)
        {
            throw new BadRequestException(
                FileErrors.TooLarge,
                $"File size exceeds the maximum allowed size of {_fileStorageOptions.MaxFileSizeMb} MB.");
        }
    }

    private async Task ValidateFileSignatureAsync(
        string storagePath,
        string normalizedExtension,
        CancellationToken cancellationToken)
    {
        await using var stream = await _storageProvider.OpenReadAsync(storagePath, cancellationToken);

        if (await FileSignatureValidator.MatchesExpectedSignatureAsync(stream, normalizedExtension, cancellationToken))
        {
            return;
        }

        throw new BadRequestException(
            FileErrors.ContentTypeNotAllowed,
            "File content does not match the declared file type.");
    }

    private async Task<FileResource> GetFileForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var fileResource = await _unitOfWork.Repository<FileResource, Guid>()
            .Query()
            .FirstOrDefaultAsync(file => file.Id == id && !file.IsDeleted, cancellationToken);

        if (fileResource is null)
        {
            throw new NotFoundException(
                FileErrors.NotFound,
                $"File with id '{id}' was not found.");
        }

        return fileResource;
    }

    private static FileResourceDetailResponse MapDetail(FileResource fileResource) =>
        new()
        {
            Id = fileResource.Id,
            OriginalFileName = fileResource.OriginalFileName,
            FileExtension = fileResource.FileExtension,
            ContentType = fileResource.ContentType,
            SizeInBytes = fileResource.SizeInBytes,
            StorageProvider = fileResource.StorageProvider,
            PublicUrl = fileResource.PublicUrl,
            ModuleName = fileResource.ModuleName,
            ReferenceType = fileResource.ReferenceType,
            ReferenceId = fileResource.ReferenceId,
            Description = fileResource.Description,
            IsTemporary = fileResource.IsTemporary,
            ExpiresAt = fileResource.ExpiresAt,
            IsActive = fileResource.IsActive,
            CreatedAt = fileResource.CreatedAt,
            Checksum = fileResource.Checksum,
            CreatedBy = fileResource.CreatedBy,
            UpdatedAt = fileResource.UpdatedAt,
            UpdatedBy = fileResource.UpdatedBy
        };

    private void InvalidateFileResourceCache(Guid id) =>
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.FileResourceDetail(id));
}
