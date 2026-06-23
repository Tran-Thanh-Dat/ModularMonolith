using Identity.Domain.PasswordResetTokens;

namespace Identity.Application.Abstractions;

public interface IPasswordResetTokenRepository
{
    Task<PasswordResetToken?> FindActiveByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default);

    Task InvalidateAllActiveForUserAsync(
        Guid userId,
        DateTimeOffset invalidatedAt,
        CancellationToken cancellationToken = default);
}
