using Notifications.Application.Abstractions;

namespace Notifications.Infrastructure.Tests.Fakes;

public sealed class FakeEmailSender : IEmailSender
{
    public bool ShouldFail { get; init; }

    public string? FailureMessage { get; init; }

    public SendEmailRequest? LastRequest { get; private set; }

    public int SendCount { get; private set; }

    public Task<SendEmailResult> SendAsync(SendEmailRequest request, CancellationToken cancellationToken = default)
    {
        SendCount++;
        LastRequest = request;

        if (ShouldFail)
        {
            return Task.FromResult(new SendEmailResult
            {
                IsSuccess = false,
                Provider = "Fake",
                ErrorMessage = FailureMessage ?? "SMTP connection failed."
            });
        }

        return Task.FromResult(new SendEmailResult
        {
            IsSuccess = true,
            Provider = "Fake",
            SentAt = DateTimeOffset.UtcNow
        });
    }
}
