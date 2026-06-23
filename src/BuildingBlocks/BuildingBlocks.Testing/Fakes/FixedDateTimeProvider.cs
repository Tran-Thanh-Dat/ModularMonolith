using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Testing.Fakes;

public sealed class FixedDateTimeProvider : IDateTimeProvider
{
    public FixedDateTimeProvider(DateTimeOffset utcNow)
    {
        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}
