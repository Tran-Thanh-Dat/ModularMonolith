namespace AuditLogs.Domain.Constants;

public static class SensitiveAuditFields
{
    public static readonly HashSet<string> FieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "PasswordHash",
        "RefreshToken",
        "RefreshTokenHash",
        "AccessToken",
        "Token",
        "TokenHash",
        "Secret",
        "ApiKey",
        "Authorization",
        "SecurityStamp",
        "ConcurrencyStamp"
    };

    private static readonly string[] SensitivePatterns =
    [
        "password",
        "token",
        "secret",
        "key",
        "hash",
        "stamp"
    ];

    public static bool IsSensitive(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return false;
        }

        if (FieldNames.Contains(fieldName))
        {
            return true;
        }

        return SensitivePatterns.Any(pattern =>
            fieldName.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
}
