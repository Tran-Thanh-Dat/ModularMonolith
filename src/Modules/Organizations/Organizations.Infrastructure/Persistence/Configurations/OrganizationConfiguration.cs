using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Domain.Constants;
using OrganizationEntity = Organizations.Domain.Organizations.Organization;

namespace Organizations.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable("organizations", OrganizationsConstants.SchemaName);
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Code).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(255).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(1000);
        builder.Property(o => o.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(o => o.SortOrder).HasDefaultValue(0).IsRequired();
        builder.Property(o => o.Metadata).HasColumnType("jsonb");
        builder.Property(o => o.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(o => o.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(o => o.CreatedBy).HasColumnName("created_by");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at");
        builder.Property(o => o.UpdatedBy).HasColumnName("updated_by");
        builder.Property(o => o.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at");
        builder.Property(o => o.DeletedBy).HasColumnName("deleted_by");

        builder.HasOne<Organizations.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrganizationEntity>()
            .WithMany()
            .HasForeignKey(o => o.ParentOrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => o.TenantId);
        builder.HasIndex(o => o.ParentOrganizationId);
        builder.HasIndex(o => new { o.TenantId, o.Code }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(o => o.IsActive);
        builder.HasIndex(o => o.Type);
        builder.HasIndex(o => o.SortOrder);
    }
}
