namespace Users.Application.Users.GetUsers;

public sealed class UserListItemResponse
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;

    public bool IsActive { get; init; }

    public DateTimeOffset? LastLoginAt { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public DateTimeOffset CreatedAt { get; init; }
}
