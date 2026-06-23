using Identity.Application.Abstractions;

namespace Identity.Infrastructure.Authentication;

public sealed class PasswordResetOptions : IPasswordResetSettings
{
    public const string SectionName = "Account:PasswordReset";

    public int ExpirationMinutes { get; set; } = 60;

    public string FrontendResetUrl { get; set; } = "http://localhost:3000/reset-password";
}
