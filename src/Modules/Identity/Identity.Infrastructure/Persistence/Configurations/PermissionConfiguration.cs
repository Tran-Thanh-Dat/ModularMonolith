using Identity.Domain.Constants;
using Identity.Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions", IdentityConstants.SchemaName);

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code).HasMaxLength(150).IsRequired();
        builder.Property(permission => permission.Name).HasMaxLength(255).IsRequired();
        builder.Property(permission => permission.Module).HasMaxLength(100).IsRequired();
        builder.Property(permission => permission.Description).HasColumnType("text");

        builder.HasIndex(permission => permission.Code).IsUnique();
    }
}
