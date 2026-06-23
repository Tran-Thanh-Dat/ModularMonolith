namespace Notifications.Application.Options;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Smtp";

    public string FromName { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;

    public SmtpOptions Smtp { get; set; } = new();

    public EmailTestModeOptions TestMode { get; set; } = new();
}

public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;

    public bool UseDefaultCredentials { get; set; }
}

public sealed class EmailTestModeOptions
{
    public bool Enabled { get; set; }

    public string RedirectTo { get; set; } = string.Empty;
}
