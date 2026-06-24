using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Organizations.Domain.Constants;
using Organizations.Domain.WorkspaceUsers;

namespace Organizations.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceUserConfiguration : IEntityTypeConfiguration<WorkspaceUser>
{
    public void Configure(EntityTypeBuilder<WorkspaceUser> builder)
    {
        builder.ToTable("workspace_users", OrganizationsConstants.SchemaName);
        builder.HasKey(m => m.Id);

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

        builder.HasOne<Organizations.Domain.Workspaces.Workspace>()
            .WithMany()
            .HasForeignKey(m => m.WorkspaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Organizations.Domain.Tenants.Tenant>()
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.TenantId);
        builder.HasIndex(m => m.WorkspaceId);
        builder.HasIndex(m => m.UserId);
        builder.HasIndex(m => new { m.WorkspaceId, m.UserId }).IsUnique().HasFilter("\"is_deleted\" = false");
    }
}
