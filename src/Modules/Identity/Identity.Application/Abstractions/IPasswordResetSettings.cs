namespace Identity.Application.Abstractions;

public interface IPasswordResetSettings
{
    int ExpirationMinutes { get; }

    string FrontendResetUrl { get; }
}
