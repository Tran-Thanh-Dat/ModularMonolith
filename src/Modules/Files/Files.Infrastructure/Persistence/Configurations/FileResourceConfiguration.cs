using Files.Domain.Constants;
using Files.Domain.FileResources;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Files.Infrastructure.Persistence.Configurations;

public sealed class FileResourceConfiguration : IEntityTypeConfiguration<FileResource>
{
    public void Configure(EntityTypeBuilder<FileResource> builder)
    {
        builder.ToTable("file_resources", FilesConstants.SchemaName);

        builder.HasKey(file => file.Id);

        builder.Property(file => file.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.StoredFileName).HasMaxLength(255).IsRequired();
        builder.Property(file => file.FileExtension).HasMaxLength(20).IsRequired();
        builder.Property(file => file.ContentType).HasMaxLength(255).IsRequired();
        builder.Property(file => file.SizeInBytes).IsRequired();
        builder.Property(file => file.StorageProvider).HasMaxLength(50).IsRequired();
        builder.Property(file => file.StoragePath).HasMaxLength(1000).IsRequired();
        builder.Property(file => file.PublicUrl).HasMaxLength(2000);
        builder.Property(file => file.Checksum).HasMaxLength(128).IsRequired();
        builder.Property(file => file.ModuleName).HasMaxLength(100);
        builder.Property(file => file.ReferenceType).HasMaxLength(100);
        builder.Property(file => file.ReferenceId).HasMaxLength(100);
        builder.Property(file => file.Description).HasMaxLength(1000);
        builder.Property(file => file.IsTemporary).HasDefaultValue(false).IsRequired();
        builder.Property(file => file.ExpiresAt);
        builder.Property(file => file.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(file => file.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(file => file.CreatedBy).HasColumnName("created_by");
        builder.Property(file => file.UpdatedAt).HasColumnName("updated_at");
        builder.Property(file => file.UpdatedBy).HasColumnName("updated_by");
        builder.Property(file => file.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(file => file.DeletedAt).HasColumnName("deleted_at");
        builder.Property(file => file.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(file => file.ModuleName);
        builder.HasIndex(file => file.ReferenceType);
        builder.HasIndex(file => file.ReferenceId);
        builder.HasIndex(file => file.ContentType);
        builder.HasIndex(file => file.FileExtension);
        builder.HasIndex(file => file.IsTemporary);
        builder.HasIndex(file => file.IsActive);
        builder.HasIndex(file => file.IsDeleted);
        builder.HasIndex(file => file.CreatedAt);
    }
}
