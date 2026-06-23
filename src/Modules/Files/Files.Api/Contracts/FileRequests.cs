using Microsoft.AspNetCore.Http;

namespace Files.Api.Contracts;

public sealed class UploadFileRequest
{
    public IFormFile File { get; init; } = default!;

    public string? ModuleName { get; init; }

    public string? ReferenceType { get; init; }

    public string? ReferenceId { get; init; }

    public string? Description { get; init; }

    public bool IsTemporary { get; init; }
}
