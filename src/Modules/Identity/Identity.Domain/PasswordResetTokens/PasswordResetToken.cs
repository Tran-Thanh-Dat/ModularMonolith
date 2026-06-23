using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Identity.Domain.PasswordResetTokens;

public sealed class PasswordResetToken : Entity
{
    private PasswordResetToken()
    {
    }

    private PasswordResetToken(
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

    public DateTimeOffset CreatedAt { get; private set; }

    public string? CreatedByIp { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    public bool IsUsed => UsedAt.HasValue;

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

    public bool IsActive => !IsUsed && !IsExpired;

    public static PasswordResetToken Create(
        Guid userId,
        string tokenHash,
        DateTimeOffset expiresAt,
        DateTimeOffset createdAt,
        string? createdByIp = null)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException("User id is required.", "PasswordResetToken.InvalidUser");
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new DomainException("Token hash is required.", "PasswordResetToken.InvalidHash");
        }

        return new PasswordResetToken(
            Guid.NewGuid(),
            userId,
            tokenHash,
            expiresAt,
            createdAt,
            createdByIp);
    }

    public void MarkUsed(DateTimeOffset usedAt) => UsedAt = usedAt;
}
