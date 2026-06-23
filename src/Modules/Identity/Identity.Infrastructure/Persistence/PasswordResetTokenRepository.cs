using Identity.Application.Abstractions;
using Identity.Domain.PasswordResetTokens;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IdentityUnitOfWork _unitOfWork;

    public PasswordResetTokenRepository(IdentityUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<PasswordResetToken?> FindActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<PasswordResetToken, Guid>()
            .Query()
            .FirstOrDefaultAsync(
                token => token.TokenHash == tokenHash && token.UsedAt == null && token.ExpiresAt > DateTimeOffset.UtcNow,
                cancellationToken);

    public Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        _unitOfWork.Repository<PasswordResetToken, Guid>().AddAsync(token, cancellationToken);

    public async Task InvalidateAllActiveForUserAsync(
        Guid userId,
        DateTimeOffset invalidatedAt,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _unitOfWork.Repository<PasswordResetToken, Guid>()
            .Query()
            .Where(token => token.UserId == userId && token.UsedAt == null && token.ExpiresAt > invalidatedAt)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.MarkUsed(invalidatedAt);
        }
    }
}
