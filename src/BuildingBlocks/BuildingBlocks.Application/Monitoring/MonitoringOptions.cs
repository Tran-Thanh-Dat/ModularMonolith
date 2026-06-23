namespace BuildingBlocks.Application.Monitoring;

public sealed class MonitoringOptions
{
    public const string SectionName = "Monitoring";

    public bool EnableRequestLogging { get; set; } = true;

    public bool EnableCorrelationId { get; set; } = true;

    public int SlowRequestThresholdMs { get; set; } = 1000;

    public bool IncludeRequestBody { get; set; }

    public bool IncludeResponseBody { get; set; }
}
