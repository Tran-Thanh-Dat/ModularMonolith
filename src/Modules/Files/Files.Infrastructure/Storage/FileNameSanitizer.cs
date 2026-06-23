namespace Files.Infrastructure.Storage;

internal static class FileNameSanitizer
{
    public static string SanitizeOriginalFileName(string fileName)
    {
        var sanitized = Path.GetFileName(fileName);

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            sanitized = sanitized.Replace(invalidChar, '_');
        }

        sanitized = sanitized.Replace('/', '_').Replace('\\', '_');

        return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized.Trim();
    }
}
