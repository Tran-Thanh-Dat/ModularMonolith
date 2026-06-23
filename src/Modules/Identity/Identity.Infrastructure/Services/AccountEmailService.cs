using Identity.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Notifications.Application.Abstractions;

namespace Identity.Infrastructure.Services;

public sealed class AccountEmailService : IAccountEmailService
{
    private readonly IEmailService _emailService;
    private readonly ILogger<AccountEmailService> _logger;

    public AccountEmailService(IEmailService emailService, ILogger<AccountEmailService> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string resetLink,
        CancellationToken cancellationToken = default)
    {
        var response = await _emailService.SendEmailAsync(
            toEmail,
            cc: null,
            bcc: null,
            subject: "Reset your password",
            body: BuildBody(fullName, resetLink),
            isHtml: true,
            referenceType: "Account.PasswordReset",
            referenceId: toEmail,
            cancellationToken);

        if (!response.IsSuccess)
        {
            _logger.LogWarning(
                "Password reset email failed for {Email}: {Error}",
                toEmail,
                response.ErrorMessage);

            throw new InvalidOperationException(response.ErrorMessage ?? "Failed to send password reset email.");
        }
    }

    private static string BuildBody(string fullName, string resetLink) =>
        $"""
         <p>Hello {System.Net.WebUtility.HtmlEncode(fullName)},</p>
         <p>We received a request to reset your password. Click the link below to choose a new password:</p>
         <p><a href="{System.Net.WebUtility.HtmlEncode(resetLink)}">Reset password</a></p>
         <p>This link expires soon. If you did not request this, you can ignore this email.</p>
         """;
}
