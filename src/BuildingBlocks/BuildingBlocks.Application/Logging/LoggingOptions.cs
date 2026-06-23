namespace BuildingBlocks.Application.Logging;

public sealed class LoggingOptions
{
    public const string SectionName = "LoggingOptions";

    public bool EnableRequestBodyLogging { get; set; }

    public bool EnableResponseBodyLogging { get; set; }

    public int MaxBodyLogSize { get; set; } = 4096;

    public string[] ExcludedPaths { get; set; } =
    [
        "/health",
        "/swagger"
    ];

    public string[] SensitiveFields { get; set; } = [];

    public string[] SensitiveHeaders { get; set; } =
    [
        "Authorization",
        "Cookie",
        "Set-Cookie"
    ];
}
