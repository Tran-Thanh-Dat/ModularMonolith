namespace Files.Application.Abstractions;

public sealed class StoredFileResult
{
    public string StoredFileName { get; init; } = default!;

    public string StoragePath { get; init; } = default!;

    public string? PublicUrl { get; init; }

    public long SizeInBytes { get; init; }

    public string ContentType { get; init; } = default!;

    public string Checksum { get; init; } = default!;
}
