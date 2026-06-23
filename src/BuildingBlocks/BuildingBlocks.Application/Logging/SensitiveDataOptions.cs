namespace BuildingBlocks.Application.Logging;

public sealed class SensitiveDataOptions
{
    public const string SectionName = "SensitiveData";

    public string[] SensitiveFields { get; set; } =
    [
        "password",
        "passwordHash",
        "accessToken",
        "refreshToken",
        "refreshTokenHash",
        "token",
        "tokenHash",
        "secret",
        "apiKey",
        "authorization",
        "cookie",
        "setCookie",
        "set-cookie",
        "securityStamp",
        "concurrencyStamp",
        "otp",
        "privateKey"
    ];

    public string[] SensitivePatterns { get; set; } =
    [
        "password",
        "token",
        "secret",
        "key",
        "hash",
        "stamp"
    ];
}
