using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Domain.Constants;
using Organizations.Domain.Tenants;

namespace Organizations.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", OrganizationsConstants.SchemaName);
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code).HasMaxLength(100).IsRequired();
        builder.Property(t => t.Name).HasMaxLength(255).IsRequired();
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.Metadata).HasColumnType("jsonb");
        builder.Property(t => t.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(t => t.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(t => t.CreatedBy).HasColumnName("created_by");
        builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by");
        builder.Property(t => t.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(t => t.DeletedAt).HasColumnName("deleted_at");
        builder.Property(t => t.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(t => t.Code).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(t => t.IsActive);
        builder.HasIndex(t => t.CreatedAt);
    }
}
