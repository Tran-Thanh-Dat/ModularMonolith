using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Identity.Domain.RefreshTokens;

public sealed class UserRefreshToken : Entity
{
    private UserRefreshToken()
    {
    }

    private UserRefreshToken(
        Guid id,
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? createdByIp)
        : base(id)
    {
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; } = default!;

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedByIp { get; private set; }

    public string? RevokedByIp { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsActive => !IsRevoked && !IsExpired;

    public static UserRefreshToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.", "RefreshToken.InvalidUser");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.", "RefreshToken.InvalidHash");
        }

        return new UserRefreshToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            createdAt,
            createdByIp);
    }

    public void Revoke(DateTimeOffset revokedAt, string? revokedByIp = null)
    {
        RevokedAt = revokedAt;
        RevokedByIp = revokedByIp;
    }
}
