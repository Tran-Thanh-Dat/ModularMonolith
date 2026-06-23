using Identity.Application.Abstractions;
using Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class IdentityRefreshTokenRepository : IIdentityRefreshTokenRepository
{
    private readonly IdentityUnitOfWork _unitOfWork;

    public IdentityRefreshTokenRepository(IdentityUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<UserRefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<UserRefreshToken, Guid>()
            .Query()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task AddAsync(UserRefreshToken refreshToken, CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<UserRefreshToken, Guid>().AddAsync(refreshToken, cancellationToken);

    public async Task RevokeAllActiveForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        string? revokedByIp = null,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _unitOfWork.Repository<UserRefreshToken, Guid>()
            .Query()
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.ExpiresAt > revokedAt)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(revokedAt, revokedByIp);
        }
    }
}
