using Categories.Domain.Categories;
using Categories.Domain.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Categories.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories", CategoriesConstants.SchemaName);

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Code).HasMaxLength(50).IsRequired();
        builder.Property(category => category.Name).HasMaxLength(200).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000);
        builder.Property(category => category.SortOrder).HasDefaultValue(0).IsRequired();
        builder.Property(category => category.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(category => category.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(category => category.CreatedBy).HasColumnName("created_by");
        builder.Property(category => category.UpdatedAt).HasColumnName("updated_at");
        builder.Property(category => category.UpdatedBy).HasColumnName("updated_by");
        builder.Property(category => category.IsDeleted).HasColumnName("is_deleted").HasDefaultValue(false).IsRequired();
        builder.Property(category => category.DeletedAt).HasColumnName("deleted_at");
        builder.Property(category => category.DeletedBy).HasColumnName("deleted_by");

        builder.HasIndex(category => category.Code)
            .IsUnique()
            .HasFilter("\"is_deleted\" = false");

        builder.HasIndex(category => category.Name);
        builder.HasIndex(category => category.IsActive);
        builder.HasIndex(category => category.IsDeleted);
        builder.HasIndex(category => category.CreatedAt);
    }
}
