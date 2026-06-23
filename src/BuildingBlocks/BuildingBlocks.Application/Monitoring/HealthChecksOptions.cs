namespace BuildingBlocks.Application.Monitoring;

public sealed class HealthChecksOptions
{
    public const string SectionName = "HealthChecks";

    public bool Enabled { get; set; } = true;

    public bool DetailsEnabled { get; set; } = true;

    public int TimeoutSeconds { get; set; } = 5;

    public HealthCheckTagOptions Tags { get; set; } = new();
}

public sealed class HealthCheckTagOptions
{
    public string[] Live { get; set; } = ["live"];

    public string[] Ready { get; set; } = ["ready"];

    public string[] Dependency { get; set; } = ["dependency"];
}
