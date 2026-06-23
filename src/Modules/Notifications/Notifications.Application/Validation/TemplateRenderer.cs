using System.Net;
using System.Text.RegularExpressions;

namespace Notifications.Application.Validation;

/// <summary>
/// Safe string placeholder replacement for email templates. No script execution.
/// Syntax: {{PlaceholderName}}
/// </summary>
public static partial class TemplateRenderer
{
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    public static string Render(
        string template,
        IReadOnlyDictionary<string, string> data,
        bool encodeHtmlValues = false)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return PlaceholderRegex().Replace(template, match =>
        {
            var key = match.Groups[1].Value;

            if (!data.TryGetValue(key, out var value))
            {
                return match.Value;
            }

            return encodeHtmlValues ? WebUtility.HtmlEncode(value) : value;
        });
    }
}
