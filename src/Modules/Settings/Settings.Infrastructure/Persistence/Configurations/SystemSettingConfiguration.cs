using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Settings.Domain.Constants;
using Settings.Domain.Settings;

namespace Settings.Infrastructure.Persistence.Configurations;

public sealed class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("system_settings", SettingsConstants.SchemaName);

        builder.HasKey(setting => setting.Id);

        builder.Property(setting => setting.Key).HasMaxLength(200).IsRequired();
        builder.Property(setting => setting.Group).HasMaxLength(100).IsRequired();
        builder.Property(setting => setting.Name).HasMaxLength(200).IsRequired();
        builder.Property(setting => setting.Description).HasMaxLength(1000);
        builder.Property(setting => setting.Value).HasColumnType("text");
        builder.Property(setting => setting.DefaultValue).HasColumnType("text");
        builder.Property(setting => setting.DataType).HasMaxLength(50).IsRequired();
        builder.Property(setting => setting.IsEncrypted).HasDefaultValue(false).IsRequired();
        builder.Property(setting => setting.IsSensitive).HasDefaultValue(false).IsRequired();
        builder.Property(setting => setting.IsSystem).HasDefaultValue(false).IsRequired();
        builder.Property(setting => setting.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(setting => setting.IsEditable).HasDefaultValue(true).IsRequired();
        builder.Property(setting => setting.SortOrder).HasDefaultValue(0).IsRequired();
        builder.Property(setting => setting.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(setting => setting.CreatedBy).HasColumnName("created_by");
        builder.Property(setting => setting.UpdatedAt).HasColumnName("updated_at");
        builder.Property(setting => setting.UpdatedBy).HasColumnName("updated_by");
        builder.Property(setting => setting.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(setting => setting.DeletedAt).HasColumnName("deleted_at");
        builder.Property(setting => setting.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(setting => setting.Key)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasIndex(setting => setting.Group);
        builder.HasIndex(setting => setting.IsActive);
        builder.HasIndex(setting => setting.IsSystem);
        builder.HasIndex(setting => setting.CreatedAt);
    }
}
