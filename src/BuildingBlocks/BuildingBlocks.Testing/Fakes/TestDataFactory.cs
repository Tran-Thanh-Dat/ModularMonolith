using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Testing.Fakes;

public static class TestDataFactory
{
    public static readonly Guid DefaultUserId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    public static readonly Guid SecondaryUserId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public static FakeCurrentUserService CreateCurrentUser(
        Guid? userId = null,
        IReadOnlyCollection<string>? permissions = null) =>
        new(userId ?? DefaultUserId, permissions: permissions);

    public static FixedDateTimeProvider CreateFixedClock(DateTimeOffset? utcNow = null) =>
        new(utcNow ?? new DateTimeOffset(2026, 1, 15, 10, 0, 0, TimeSpan.Zero));
}
