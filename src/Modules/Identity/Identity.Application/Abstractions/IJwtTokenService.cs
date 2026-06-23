using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public sealed class JwtTokenResult
{
    public string AccessToken { get; init; } = default!;

    public DateTimeOffset ExpiresAt { get; init; }
}

public interface IJwtTokenService
{
    Task<JwtTokenResult> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions,
        CancellationToken cancellationToken = default);
}
