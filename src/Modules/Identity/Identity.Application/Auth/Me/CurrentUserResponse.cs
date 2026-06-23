namespace Identity.Application.Auth.Me;

public sealed class CurrentUserResponse
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}
