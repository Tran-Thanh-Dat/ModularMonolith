using Files.Application.Abstractions;

namespace Files.Infrastructure.Tests.Fakes;

public sealed class FakeFileStorageProvider : IFileStorageProvider
{
    public bool ThrowOnDelete { get; init; }

    public bool Exists { get; set; } = true;

    public byte[] FileContent { get; init; } = "%PDF-1.4\n"u8.ToArray();

    public List<string> DeletedPaths { get; } = [];

    public string ProviderName => "Fake";

    public Task<StoredFileResult> SaveAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        using var memory = new MemoryStream();
        fileStream.CopyTo(memory);
        var storagePath = $"{folder}/stored-{originalFileName}";

        return Task.FromResult(new StoredFileResult
        {
            StoredFileName = Path.GetFileName(storagePath),
            StoragePath = storagePath,
            ContentType = contentType,
            SizeInBytes = memory.Length,
            PublicUrl = null,
            Checksum = "checksum"
        });
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        if (ThrowOnDelete)
        {
            throw new IOException("Delete failed.");
        }

        DeletedPaths.Add(storagePath);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult(Exists);

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default) =>
        Task.FromResult<Stream>(new MemoryStream(FileContent));
}
