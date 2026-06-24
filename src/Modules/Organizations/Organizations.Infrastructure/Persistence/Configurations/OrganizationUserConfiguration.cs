using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Domain.Constants;
using Organizations.Domain.OrganizationUsers;

namespace Organizations.Infrastructure.Persistence.Configurations;

public sealed class OrganizationUserConfiguration : IEntityTypeConfiguration<OrganizationUser>
{
    public void Configure(EntityTypeBuilder<OrganizationUser> builder)
    {
        builder.ToTable("organization_users", OrganizationsConstants.SchemaName);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.IsDefault).HasDefaultValue(false).IsRequired();
        builder.Property(m => m.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(m => m.JoinedAt).IsRequired();
        builder.Property(m => m.LeftAt);
        builder.Property(m => m.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(m => m.CreatedBy).HasColumnName("created_by");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at");
        builder.Property(m => m.UpdatedBy).HasColumnName("updated_by");
        builder.Property(m => m.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at");
        builder.Property(m => m.DeletedBy).HasColumnName("deleted_by");

        builder.HasOne<Organizations.Domain.Organizations.Organization>()
            .WithMany()
            .HasForeignKey(m => m.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organizations.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.OrganizationId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.OrganizationId, m.UserId }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(m => new { m.TenantId, m.UserId, m.IsDefault })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false AND \"IsDefault\" = true");
    }
}
