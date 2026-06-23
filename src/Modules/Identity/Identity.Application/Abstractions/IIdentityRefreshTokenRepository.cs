using Identity.Domain.RefreshTokens;

namespace Identity.Application.Abstractions;

public interface IIdentityRefreshTokenRepository
{
    Task<UserRefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(UserRefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAllActiveForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default);
}
