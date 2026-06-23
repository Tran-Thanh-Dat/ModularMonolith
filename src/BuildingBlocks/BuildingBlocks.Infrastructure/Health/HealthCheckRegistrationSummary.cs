namespace BuildingBlocks.Infrastructure.Health;

public sealed class HealthCheckRegistrationSummary
{
    public int DependencyCheckCount { get; init; }

    public int LiveCheckCount { get; init; }
}
