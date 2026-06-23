namespace Files.Application.Validation;

public static class FileValidationRules
{
    private static readonly char[] UnsafeFileNameChars =
        Path.GetInvalidFileNameChars()
            .Concat(['\\', '/'])
            .Distinct()
            .ToArray();

    public static bool HasUnsafeFileNameCharacters(string fileName) =>
        fileName.IndexOfAny(UnsafeFileNameChars) >= 0;

    public static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().ToLowerInvariant();
        return normalized.StartsWith('.') ? normalized : $".{normalized}";
    }

    public static string GetExtensionFromFileName(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return NormalizeExtension(extension);
    }
}
