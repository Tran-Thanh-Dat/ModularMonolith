using BuildingBlocks.Application.Abstractions;

namespace BuildingBlocks.Testing.Fakes;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public FakeCurrentUserService(
        Guid? userId = null,
        string? userName = "test-user",
        bool isAuthenticated = true,
        IReadOnlyCollection<string>? permissions = null,
        IReadOnlyCollection<string>? roles = null)
    {
        UserId = userId ?? Guid.NewGuid();
        UserName = userName;
        Email = $"{userName}@example.com";
        IsAuthenticated = isAuthenticated;
        Permissions = permissions ?? [];
        Roles = roles ?? [];
    }

    public Guid? UserId { get; set; }

    public string? UserName { get; set; }

    public string? Email { get; set; }

    public bool IsAuthenticated { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; }

    public IReadOnlyCollection<string> Permissions { get; set; }
}
