namespace BuildingBlocks.Infrastructure.Health;

public sealed class ApplicationUptime
{
    public DateTimeOffset StartedAtUtc { get; } = DateTimeOffset.UtcNow;

    public TimeSpan Uptime => DateTimeOffset.UtcNow - StartedAtUtc;
}
