using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using BuildingBlocks.Application.Errors;
using Notifications.Application.Abstractions;
using Notifications.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Notifications.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _options;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SendEmailResult> SendAsync(
        SendEmailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Smtp.Host) ||
            string.IsNullOrWhiteSpace(_options.FromAddress))
        {
            return new SendEmailResult
            {
                IsSuccess = false,
                Provider = "Smtp",
                ErrorMessage = "Email provider is not configured."
            };
        }

        try
        {
            var message = BuildMimeMessage(request);

            using var client = new SmtpClient();
            await client.ConnectAsync(
                _options.Smtp.Host,
                _options.Smtp.Port,
                _options.Smtp.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!_options.Smtp.UseDefaultCredentials &&
                !string.IsNullOrWhiteSpace(_options.Smtp.UserName))
            {
                await client.AuthenticateAsync(
                    _options.Smtp.UserName,
                    _options.Smtp.Password,
                    cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            var sentAt = DateTimeOffset.UtcNow;
            _logger.LogInformation(
                "Email sent via SMTP to {To} with subject {Subject}",
                request.To,
                request.Subject);

            return new SendEmailResult
            {
                IsSuccess = true,
                Provider = "Smtp",
                SentAt = sentAt
            };
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "SMTP send failed for recipient {To} with subject {Subject}",
                request.To,
                request.Subject);

            return new SendEmailResult
            {
                IsSuccess = false,
                Provider = "Smtp",
                ErrorMessage = "Email delivery failed."
            };
        }
    }

    private MimeMessage BuildMimeMessage(SendEmailRequest request)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(request.To));
        message.Subject = request.Subject;

        if (!string.IsNullOrWhiteSpace(request.Cc))
        {
            foreach (var address in request.Cc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                message.Cc.Add(MailboxAddress.Parse(address));
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Bcc))
        {
            foreach (var address in request.Bcc.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                message.Bcc.Add(MailboxAddress.Parse(address));
            }
        }

        var bodyBuilder = new BodyBuilder();
        if (request.IsHtml)
        {
            bodyBuilder.HtmlBody = request.Body;
        }
        else
        {
            bodyBuilder.TextBody = request.Body;
        }

        if (!string.IsNullOrWhiteSpace(request.OriginalTo))
        {
            bodyBuilder.TextBody = $"[TestMode] Original To: {request.OriginalTo}\n" +
                                   (string.IsNullOrWhiteSpace(request.OriginalCc) ? string.Empty : $"Original Cc: {request.OriginalCc}\n") +
                                   (string.IsNullOrWhiteSpace(request.OriginalBcc) ? string.Empty : $"Original Bcc: {request.OriginalBcc}\n\n") +
                                   bodyBuilder.TextBody;
            if (request.IsHtml)
            {
                bodyBuilder.HtmlBody = $"<p><strong>[TestMode]</strong> Original To: {request.OriginalTo}</p>" + bodyBuilder.HtmlBody;
            }
        }

        message.Body = bodyBuilder.ToMessageBody();
        return message;
    }
}
