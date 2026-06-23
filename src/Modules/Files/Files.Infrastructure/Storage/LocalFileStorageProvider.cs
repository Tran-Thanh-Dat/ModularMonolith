using System.Security.Cryptography;
using Files.Application.Abstractions;
using Files.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Files.Infrastructure.Storage;

public sealed class LocalFileStorageProvider : IFileStorageProvider
{
    private readonly string _rootPath;
    private readonly string? _publicBaseUrl;
    private readonly ILogger<LocalFileStorageProvider> _logger;

    public LocalFileStorageProvider(
        IOptions<FileStorageOptions> options,
        IHostEnvironment hostEnvironment,
        ILogger<LocalFileStorageProvider> logger)
    {
        var localOptions = options.Value.Local;
        _rootPath = ResolveRootPath(localOptions.RootPath, hostEnvironment.ContentRootPath);
        _publicBaseUrl = string.IsNullOrWhiteSpace(localOptions.PublicBaseUrl)
            ? null
            : localOptions.PublicBaseUrl.Trim().TrimEnd('/');
        _logger = logger;

        Directory.CreateDirectory(_rootPath);
    }

    public string ProviderName => "Local";

    public async Task<StoredFileResult> SaveAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        string folder,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        // TODO: Integrate antivirus scanning before persisting the file stream.
        // Basic magic-byte validation runs after save in FileService.ValidateFileSignatureAsync.

        var sanitizedOriginalFileName = FileNameSanitizer.SanitizeOriginalFileName(originalFileName);
        var extension = Path.GetExtension(sanitizedOriginalFileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".bin";
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativeFolder = NormalizeRelativePath(folder);
        var relativePath = Path.Combine(relativeFolder, storedFileName).Replace('\\', '/');
        var absoluteDirectory = GetSafeAbsolutePath(relativeFolder);
        Directory.CreateDirectory(absoluteDirectory);

        var absoluteFilePath = Path.Combine(absoluteDirectory, storedFileName);
        long sizeInBytes;
        string checksum;

        await using (var output = new FileStream(
                         absoluteFilePath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 81920,
                         useAsync: true))
        using (var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
        {
            var buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                incrementalHash.AppendData(buffer, 0, bytesRead);
            }

            sizeInBytes = output.Length;
            checksum = Convert.ToHexString(incrementalHash.GetHashAndReset());
        }

        _logger.LogInformation(
            "Stored file {StoredFileName} ({SizeInBytes} bytes) at relative path {StoragePath}",
            storedFileName,
            sizeInBytes,
            relativePath);

        return new StoredFileResult
        {
            StoredFileName = storedFileName,
            StoragePath = relativePath,
            PublicUrl = BuildPublicUrl(relativePath),
            SizeInBytes = sizeInBytes,
            ContentType = contentType,
            Checksum = checksum
        };
    }

    public Task<Stream> OpenReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetSafeAbsolutePath(NormalizeRelativePath(storagePath));
        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException("Stored file was not found.", absolutePath);
        }

        Stream stream = new FileStream(
            absolutePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        return Task.FromResult(stream);
    }

    public Task<bool> ExistsAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetSafeAbsolutePath(NormalizeRelativePath(storagePath));
        return Task.FromResult(File.Exists(absolutePath));
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetSafeAbsolutePath(NormalizeRelativePath(storagePath));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private string? BuildPublicUrl(string relativePath)
    {
        if (_publicBaseUrl is null)
        {
            return null;
        }

        return $"{_publicBaseUrl}/{relativePath.Replace('\\', '/')}";
    }

    private string GetSafeAbsolutePath(string relativePath)
    {
        var normalizedRelativePath = NormalizeRelativePath(relativePath);
        var combined = Path.GetFullPath(Path.Combine(_rootPath, normalizedRelativePath));
        var normalizedRoot = Path.GetFullPath(_rootPath);

        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid storage path.");
        }

        return combined;
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath
            .Replace('\\', '/')
            .Trim('/')
            .Replace('/', Path.DirectorySeparatorChar);

    private static string ResolveRootPath(string configuredRootPath, string contentRootPath)
    {
        if (Path.IsPathRooted(configuredRootPath))
        {
            return Path.GetFullPath(configuredRootPath);
        }

        return Path.GetFullPath(Path.Combine(contentRootPath, configuredRootPath));
    }
}
