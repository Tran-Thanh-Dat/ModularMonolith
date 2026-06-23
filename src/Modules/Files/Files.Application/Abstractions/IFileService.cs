using BuildingBlocks.Application.Pagination;
using Files.Application.Files;

namespace Files.Application.Abstractions;

public interface IFileService
{
    Task<UploadFileResponse> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string? moduleName,
        string? referenceType,
        string? referenceId,
        string? description,
        bool isTemporary,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task MarkAsPermanentAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FileResourceDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<FileResourceListItemResponse>> GetListAsync(
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
        CancellationToken cancellationToken = default);

    Task<FileDownloadResult> DownloadAsync(Guid id, CancellationToken cancellationToken = default);
}
