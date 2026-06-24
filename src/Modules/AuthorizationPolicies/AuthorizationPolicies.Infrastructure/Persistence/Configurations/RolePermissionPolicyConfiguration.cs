using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.RolePermissionPolicies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthorizationPolicies.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionPolicyConfiguration : IEntityTypeConfiguration<RolePermissionPolicy>
{
    public void Configure(EntityTypeBuilder<RolePermissionPolicy> builder)
    {
        builder.ToTable("role_permission_policies", AuthorizationPoliciesConstants.SchemaName);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.RoleId).IsRequired();
        builder.Property(p => p.PermissionPolicyId).IsRequired();
        builder.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(p => p.RoleId);
        builder.HasIndex(p => p.PermissionPolicyId);
        builder.HasIndex(p => new { p.RoleId, p.PermissionPolicyId }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(p => p.IsActive);

        builder.HasOne<Domain.PermissionPolicies.PermissionPolicy>()
            .WithMany()
            .HasForeignKey(p => p.PermissionPolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
