namespace Files.Application.Validation;

/// <summary>
/// Basic file header checks for common upload types.
/// Extension and content-type whitelist alone are not strong file-type verification.
/// TODO: Expand magic-byte validation and integrate antivirus scanning for production.
/// </summary>
public static class FileSignatureValidator
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();

    public static async Task<bool> MatchesExpectedSignatureAsync(
        Stream stream,
        string normalizedExtension,
        CancellationToken cancellationToken = default)
    {
        if (!stream.CanRead)
        {
            return false;
        }

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        var header = new byte[8];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), cancellationToken);

        if (stream.CanSeek)
        {
            stream.Position = 0;
        }

        return normalizedExtension switch
        {
            ".pdf" => StartsWith(header, bytesRead, PdfSignature),
            ".png" => StartsWith(header, bytesRead, PngSignature),
            ".jpg" or ".jpeg" => StartsWith(header, bytesRead, JpegSignature),
            ".docx" or ".xlsx" => StartsWith(header, bytesRead, ZipSignature),
            ".csv" => bytesRead > 0,
            _ => true
        };
    }

    private static bool StartsWith(byte[] buffer, int length, byte[] signature)
    {
        if (length < signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (buffer[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }
}
