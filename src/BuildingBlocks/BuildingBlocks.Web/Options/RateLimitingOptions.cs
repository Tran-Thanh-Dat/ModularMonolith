namespace BuildingBlocks.Web.Options;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public bool Enabled { get; set; } = true;

    public int GeneralPermitLimit { get; set; } = 120;

    public int GeneralWindowSeconds { get; set; } = 60;

    public int LoginPermitLimit { get; set; } = 5;

    public int LoginWindowSeconds { get; set; } = 60;

    public int RefreshPermitLimit { get; set; } = 10;

    public int RefreshWindowSeconds { get; set; } = 60;

    public int AuthPermitLimit { get; set; } = 10;

    public int AuthWindowSeconds { get; set; } = 60;

    public int FileUploadPermitLimit { get; set; } = 20;

    public int FileUploadWindowSeconds { get; set; } = 60;

    public int BackgroundJobsPermitLimit { get; set; } = 10;

    public int BackgroundJobsWindowSeconds { get; set; } = 60;
}
