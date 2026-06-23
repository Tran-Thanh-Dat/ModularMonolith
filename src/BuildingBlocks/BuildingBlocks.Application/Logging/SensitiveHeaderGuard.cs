namespace BuildingBlocks.Application.Logging;

public static class SensitiveHeaderGuard
{
    private static readonly HashSet<string> DefaultSensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie"
    };

    public static bool IsSensitiveHeader(string headerName) =>
        DefaultSensitiveHeaders.Contains(headerName);

    public static bool ContainsSensitiveHeader(IEnumerable<string> headerNames) =>
        headerNames.Any(IsSensitiveHeader);
}
