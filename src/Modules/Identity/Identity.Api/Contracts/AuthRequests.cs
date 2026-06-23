namespace Identity.Api.Contracts;

public sealed class LoginRequest
{
    public string UserNameOrEmail { get; init; } = default!;

    public string Password { get; init; } = default!;

    public bool RememberMe { get; init; }
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; init; } = default!;
}

public sealed class LogoutRequest
{
    public string RefreshToken { get; init; } = default!;
}
