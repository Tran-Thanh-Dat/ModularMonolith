using Identity.Domain.Constants;
using Identity.Domain.RefreshTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserRefreshTokenConfiguration : IEntityTypeConfiguration<UserRefreshToken>
{
    public void Configure(EntityTypeBuilder<UserRefreshToken> builder)
    {
        builder.ToTable("user_refresh_tokens", IdentityConstants.SchemaName);

        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasColumnType("text").IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(token => token.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(100);
        builder.Property(token => token.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(100);

        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.ExpiresAt);
        builder.HasIndex(token => token.TokenHash);

        builder.Ignore(token => token.IsRevoked);
        builder.Ignore(token => token.IsExpired);
        builder.Ignore(token => token.IsActive);
    }
}
