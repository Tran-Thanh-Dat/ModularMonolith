using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.PermissionPolicies;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthorizationPolicies.Infrastructure.Persistence.Configurations;

public sealed class PermissionPolicyConfiguration : IEntityTypeConfiguration<PermissionPolicy>
{
    public void Configure(EntityTypeBuilder<PermissionPolicy> builder)
    {
        builder.ToTable("permission_policies", AuthorizationPoliciesConstants.SchemaName);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.PermissionCode).HasMaxLength(150).IsRequired();
        builder.Property(p => p.ModuleCode).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Conditions).HasColumnType("jsonb");
        builder.Property(p => p.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(p => p.Code).IsUnique().HasFilter("\"is_deleted\" = false");
        builder.HasIndex(p => p.PermissionCode);
        builder.HasIndex(p => p.ModuleCode);
        builder.HasIndex(p => p.ResourceType);
        builder.HasIndex(p => p.Action);
        builder.HasIndex(p => p.Scope);
        builder.HasIndex(p => p.Effect);
        builder.HasIndex(p => p.IsActive);
    }
}
