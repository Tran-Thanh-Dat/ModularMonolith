using MasterData.Domain.Constants;
using MasterData.Domain.Entities;
using MasterData.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MasterData.Infrastructure.Persistence.Configurations;

public sealed class MasterDataGroupConfiguration : IEntityTypeConfiguration<MasterDataGroup>
{
    public void Configure(EntityTypeBuilder<MasterDataGroup> builder)
    {
        builder.ToTable("master_data_groups", MasterDataConstants.SchemaName);
        builder.HasKey(g => g.Id);

        builder.Property(g => g.Code).HasMaxLength(100).IsRequired();
        builder.Property(g => g.Name).HasMaxLength(255).IsRequired();
        builder.Property(g => g.Description).HasMaxLength(1000);
        builder.Property(g => g.Scope).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(g => g.IsSystem).HasDefaultValue(false).IsRequired();
        builder.Property(g => g.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(g => g.SortOrder).HasDefaultValue(0).IsRequired();
        builder.Property(g => g.Metadata).HasColumnType("jsonb");
        builder.Property(g => g.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(g => g.CreatedBy).HasColumnName("created_by");
        builder.Property(g => g.UpdatedAt).HasColumnName("updated_at");
        builder.Property(g => g.UpdatedBy).HasColumnName("updated_by");
        builder.Property(g => g.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(g => g.DeletedAt).HasColumnName("deleted_at");
        builder.Property(g => g.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(g => g.Code);
        builder.HasIndex(g => g.Scope);
        builder.HasIndex(g => g.TenantId);
        builder.HasIndex(g => g.OrganizationId);
        builder.HasIndex(g => g.IsSystem);
        builder.HasIndex(g => g.IsActive);
        builder.HasIndex(g => g.CreatedAt);

        builder.HasIndex(g => new { g.Scope, g.TenantId, g.OrganizationId, g.Code })
            .IsUnique()
            .HasDatabaseName("ix_master_data_groups_scope_tenant_org_code")
            .HasFilter("\"is_deleted\" = false");
    }
}
