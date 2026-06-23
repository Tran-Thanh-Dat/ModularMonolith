using System.Text.RegularExpressions;

namespace BuildingBlocks.Infrastructure.Health;

public static partial class HealthCheckDescriptionSanitizer
{
    public static string? SanitizeForPublicResponse(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (ShouldRedactPublicDescription(description))
        {
            return "Check completed.";
        }

        return description;
    }

    public static string? SanitizeForAdminResponse(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        if (ContainsSecretIndicators(description))
        {
            return "Check completed.";
        }

        return description;
    }

    private static bool ShouldRedactPublicDescription(string description) =>
        ContainsSecretIndicators(description) ||
        ContainsOperationalDetails(description);

    private static bool ContainsSecretIndicators(string description) =>
        description.Contains("password", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("connection string", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("User ID=", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsOperationalDetails(string description) =>
        HostPortPattern().IsMatch(description) ||
        IpAddressPattern().IsMatch(description) ||
        FilePathPattern().IsMatch(description) ||
        description.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("server(s)", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
        description.Contains("stack", StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@":\d{2,5}(?!\d)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex HostPortPattern();

    [GeneratedRegex(@"\b\d{1,3}(\.\d{1,3}){3}\b", RegexOptions.CultureInvariant)]
    private static partial Regex IpAddressPattern();

    [GeneratedRegex(@"([A-Za-z]:\\|/|\\)[\w\\./-]+", RegexOptions.CultureInvariant)]
    private static partial Regex FilePathPattern();
}
