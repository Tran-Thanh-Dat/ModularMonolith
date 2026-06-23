namespace Files.Application.Files;

public class FileResourceListItemResponse
{
    public Guid Id { get; init; }

    public string OriginalFileName { get; init; } = default!;

    public string FileExtension { get; init; } = default!;

    public string ContentType { get; init; } = default!;

    public long SizeInBytes { get; init; }

    public string StorageProvider { get; init; } = default!;

    public string? PublicUrl { get; init; }

    public string? ModuleName { get; init; }

    public string? ReferenceType { get; init; }

    public string? ReferenceId { get; init; }

    public string? Description { get; init; }

    public bool IsTemporary { get; init; }

    public DateTimeOffset? ExpiresAt { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class FileResourceDetailResponse : FileResourceListItemResponse
{
    public string Checksum { get; init; } = default!;

    public Guid? CreatedBy { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public Guid? UpdatedBy { get; init; }
}

public sealed class UploadFileResponse
{
    public Guid Id { get; init; }

    public string OriginalFileName { get; init; } = default!;

    public string ContentType { get; init; } = default!;

    public long SizeInBytes { get; init; }

    public string? PublicUrl { get; init; }
}

public sealed class FileDownloadResult
{
    public Stream Content { get; init; } = default!;

    public string ContentType { get; init; } = default!;

    public string FileName { get; init; } = default!;
}
