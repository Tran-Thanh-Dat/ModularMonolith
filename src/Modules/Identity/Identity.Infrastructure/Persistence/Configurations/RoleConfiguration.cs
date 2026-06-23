using Identity.Domain.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles", IdentityConstants.SchemaName);

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Code).HasMaxLength(100).IsRequired();
        builder.Property(role => role.Name).HasMaxLength(255).IsRequired();
        builder.Property(role => role.Description).HasColumnType("text");
        builder.Property(role => role.IsActive).IsRequired();

        builder.HasIndex(role => role.Code).IsUnique();

        builder.HasMany(role => role.Permissions)
            .WithMany()
            .UsingEntity(j => j.ToTable("role_permissions", IdentityConstants.SchemaName));

        builder.Navigation(role => role.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(role => role.Users).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
