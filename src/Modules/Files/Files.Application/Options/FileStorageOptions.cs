namespace Files.Application.Options;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>
    /// Storage provider name. Use "Local" for development.
    /// </summary>
    public string Provider { get; set; } = "Local";

    public LocalFileStorageOptions Local { get; set; } = new();

    public int MaxFileSizeMb { get; set; } = 20;

    public List<string> AllowedExtensions { get; set; } = [];

    public List<string> AllowedContentTypes { get; set; } = [];
}

/// <summary>
/// Local disk storage settings. Use a relative path for development.
/// Production should set <see cref="RootPath"/> to an absolute path outside the repository and deployment folder.
/// </summary>
public sealed class LocalFileStorageOptions
{
    public string RootPath { get; set; } = "uploads";

    public string PublicBaseUrl { get; set; } = string.Empty;
}
