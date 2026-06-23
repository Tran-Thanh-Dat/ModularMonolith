using System.Text.RegularExpressions;

namespace Notifications.Application.Validation;

public static partial class EmailValidationRules
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EmailRegex();

    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex().IsMatch(email.Trim());

    public static bool IsValidEmailList(string? emails)
    {
        if (string.IsNullOrWhiteSpace(emails))
        {
            return true;
        }

        return emails
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .All(IsValidEmail);
    }
}
