namespace Files.Application.Abstractions;

public interface IFileStorageProvider
{
    string ProviderName { get; }

    Task<StoredFileResult> SaveAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
