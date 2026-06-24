using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthorizationPolicies.Infrastructure.Persistence.Configurations;

public sealed class AuthorizationMatrixEntryConfiguration : IEntityTypeConfiguration<AuthorizationMatrixEntry>
{
    public void Configure(EntityTypeBuilder<AuthorizationMatrixEntry> builder)
    {
        builder.ToTable("authorization_matrix_entries", AuthorizationPoliciesConstants.SchemaName);
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ModuleCode).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ResourceType).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Action).HasMaxLength(100).IsRequired();
        builder.Property(p => p.RequiredPermissionCode).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Metadata).HasColumnType("jsonb");
        builder.Property(p => p.IsEnabled).HasDefaultValue(true).IsRequired();
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(p => p.CreatedBy).HasColumnName("created_by");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by");
        builder.Property(p => p.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at");
        builder.Property(p => p.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(p => p.ModuleCode);
        builder.HasIndex(p => p.ResourceType);
        builder.HasIndex(p => p.Action);
        builder.HasIndex(p => p.Scope);
        builder.HasIndex(p => p.RequiredPermissionCode);
        builder.HasIndex(p => new { p.ModuleCode, p.ResourceType, p.Action, p.Scope })
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");
        builder.HasIndex(p => p.IsEnabled);
    }
}
