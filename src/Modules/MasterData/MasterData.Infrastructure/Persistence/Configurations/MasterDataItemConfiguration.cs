using MasterData.Domain.Constants;
using MasterData.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MasterData.Infrastructure.Persistence.Configurations;

public sealed class MasterDataItemConfiguration : IEntityTypeConfiguration<MasterDataItem>
{
    public void Configure(EntityTypeBuilder<MasterDataItem> builder)
    {
        builder.ToTable("master_data_items", MasterDataConstants.SchemaName);
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Code).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Name).HasMaxLength(255).IsRequired();
        builder.Property(i => i.Value).HasMaxLength(500);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.IsSystem).HasDefaultValue(false).IsRequired();
        builder.Property(i => i.IsDefault).HasDefaultValue(false).IsRequired();
        builder.Property(i => i.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(i => i.SortOrder).HasDefaultValue(0).IsRequired();
        builder.Property(i => i.Metadata).HasColumnType("jsonb");
        builder.Property(i => i.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(i => i.CreatedBy).HasColumnName("created_by");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at");
        builder.Property(i => i.UpdatedBy).HasColumnName("updated_by");
        builder.Property(i => i.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at");
        builder.Property(i => i.DeletedBy).HasColumnName("deleted_by");

        builder.HasOne<MasterDataGroup>()
            .WithMany()
            .HasForeignKey(i => i.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<MasterDataItem>()
            .WithMany()
            .HasForeignKey(i => i.ParentItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.GroupId);
        builder.HasIndex(i => i.ParentItemId);
        builder.HasIndex(i => i.Code);
        builder.HasIndex(i => i.IsSystem);
        builder.HasIndex(i => i.IsDefault);
        builder.HasIndex(i => i.IsActive);
        builder.HasIndex(i => i.SortOrder);
        builder.HasIndex(i => i.EffectiveFrom);
        builder.HasIndex(i => i.EffectiveTo);

        builder.HasIndex(i => new { i.GroupId, i.Code })
            .IsUnique()
            .HasDatabaseName("ix_master_data_items_group_code")
            .HasFilter("\"is_deleted\" = false");

        builder.HasIndex(i => new { i.GroupId, i.IsDefault })
            .IsUnique()
            .HasDatabaseName("ix_master_data_items_group_default")
            .HasFilter("\"is_deleted\" = false AND \"IsDefault\" = true");
    }
}
