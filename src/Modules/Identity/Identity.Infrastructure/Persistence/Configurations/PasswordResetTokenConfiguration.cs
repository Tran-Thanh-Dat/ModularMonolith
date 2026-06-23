using Identity.Domain.Constants;
using Identity.Domain.PasswordResetTokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens", IdentityConstants.SchemaName);

        builder.HasKey(token => token.Id);

        builder.Property(token => token.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(token => token.TokenHash).HasColumnName("token_hash").HasColumnType("text").IsRequired();
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at").IsRequired();
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(token => token.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(100);
        builder.Property(token => token.UsedAt).HasColumnName("used_at");

        builder.HasIndex(token => token.UserId);
        builder.HasIndex(token => token.TokenHash);
        builder.HasIndex(token => token.ExpiresAt);

        builder.Ignore(token => token.IsUsed);
        builder.Ignore(token => token.IsExpired);
        builder.Ignore(token => token.IsActive);
    }
}
