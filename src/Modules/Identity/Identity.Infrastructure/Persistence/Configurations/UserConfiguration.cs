using Identity.Domain.Constants;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", IdentityConstants.SchemaName);

        builder.HasKey(user => user.Id);

        builder.Property(user => user.UserName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(255).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnType("text").IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(255).IsRequired();
        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(user => user.CreatedBy).HasColumnName("created_by");
        builder.Property(user => user.UpdatedAt).HasColumnName("updated_at");
        builder.Property(user => user.UpdatedBy).HasColumnName("updated_by");
        builder.Property(user => user.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(user => user.DeletedAt).HasColumnName("deleted_at");
        builder.Property(user => user.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(user => user.UserName).IsUnique();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.IsActive);
        builder.HasIndex(user => user.IsDeleted);

        builder.HasMany(user => user.Roles)
            .WithMany(role => role.Users)
            .UsingEntity(j => j.ToTable("user_roles", IdentityConstants.SchemaName));

        builder.HasMany(user => user.DirectPermissions)
            .WithMany()
            .UsingEntity(j => j.ToTable("user_permissions", IdentityConstants.SchemaName));

        builder.Ignore(user => user.DomainEvents);
    }
}
