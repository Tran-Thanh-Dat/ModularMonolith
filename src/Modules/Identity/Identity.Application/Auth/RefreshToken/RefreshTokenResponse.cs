namespace Identity.Application.Auth.RefreshToken;

public sealed class RefreshTokenResponse
{
    public string AccessToken { get; init; } = default!;

    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = default!;

    public DateTimeOffset RefreshTokenExpiresAt { get; init; }
}
