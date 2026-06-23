namespace Identity.Application.Auth.Login;

public sealed class LoginUserDto
{
    public Guid Id { get; init; }

    public string UserName { get; init; } = default!;

    public string Email { get; init; } = default!;

    public string FullName { get; init; } = default!;

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public IReadOnlyCollection<string> Permissions { get; init; } = [];
}

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = default!;

    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = default!;

    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public LoginUserDto User { get; init; } = default!;
}
