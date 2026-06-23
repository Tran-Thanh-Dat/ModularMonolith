using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Infrastructure.DateTime;

public sealed class DateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
