using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthorizationPolicies.Infrastructure.Persistence.Configurations;

public sealed class UserPermissionPolicyOverrideConfiguration : IEntityTypeConfiguration<UserPermissionPolicyOverride>
{
    public void Configure(EntityTypeBuilder<UserPermissionPolicyOverride> builder)
    {
        builder.ToTable("user_permission_policy_overrides", AuthorizationPoliciesConstants.SchemaName);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.UserId).IsRequired();
        builder.Property(p => p.PermissionPolicyId).IsRequired();
        builder.Property(p => p.Reason).HasMaxLength(1000);
        builder.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.PermissionPolicyId);
        builder.HasIndex(p => p.Effect);
        builder.HasIndex(p => p.ExpiresAt);
        builder.HasIndex(p => new { p.UserId, p.PermissionPolicyId }).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(p => p.IsActive);

        builder.HasOne<Domain.PermissionPolicies.PermissionPolicy>()
            .WithMany()
            .HasForeignKey(p => p.PermissionPolicyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
