namespace Identity.Application.Abstractions;

public interface IAccountEmailService
{
    Task SendPasswordResetEmailAsync(
        string toEmail,
        string fullName,
        string resetLink,
        CancellationToken cancellationToken = default);
}
