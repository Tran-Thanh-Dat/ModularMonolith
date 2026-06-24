using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Domain.Constants;
using Organizations.Domain.Workspaces;

namespace Organizations.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces", OrganizationsConstants.SchemaName);
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code).HasMaxLength(100).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(255).IsRequired();
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.Metadata).HasColumnType("jsonb");
        builder.Property(w => w.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(w => w.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(w => w.CreatedBy).HasColumnName("created_by");
        builder.Property(w => w.UpdatedAt).HasColumnName("updated_at");
        builder.Property(w => w.UpdatedBy).HasColumnName("updated_by");
        builder.Property(w => w.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(w => w.DeletedAt).HasColumnName("deleted_at");
        builder.Property(w => w.DeletedBy).HasColumnName("deleted_by");

        builder.HasOne<Organizations.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(w => w.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organizations.Domain.Organizations.Organization>()
            .WithMany()
            .HasForeignKey(w => w.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => w.OrganizationId);
        builder.HasIndex(w => new { w.TenantId, w.Code }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(w => w.IsActive);
    }
}
