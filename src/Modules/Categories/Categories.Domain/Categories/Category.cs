using BuildingBlocks.Domain.Exceptions;
using BuildingBlocks.Domain.Primitives;

namespace Categories.Domain.Categories;

public sealed class Category : SoftDeletableEntity
{
    private Category()
    {
    }

    private Category(
        Guid id,
        string code,
        string name,
        string? description,
        int sortOrder,
        DateTimeOffset createdAt,
        Guid? createdBy)
        : base(id)
    {
        Code = code;
        Name = name;
        Description = description;
        SortOrder = sortOrder;
        IsActive = true;
        SetCreated(createdBy, createdAt);
    }

    public string Code { get; private set; } = default!;

    public string Name { get; private set; } = default!;

    public string? Description { get; private set; }

    public int SortOrder { get; private set; }

    public bool IsActive { get; private set; }

    public static Category Create(
        string code,
        string name,
        string? description,
        int sortOrder,
        DateTimeOffset createdAt,
        Guid? createdBy = null)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Category code is required.", "Category.InvalidCode");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.", "Category.InvalidName");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", "Category.InvalidSortOrder");
        }

        return new Category(
            Guid.NewGuid(),
            code.Trim(),
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            sortOrder,
            createdAt,
            createdBy);
    }

    public void Update(string name, string? description, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Category name is required.", "Category.InvalidName");
        }

        if (sortOrder < 0)
        {
            throw new DomainException("Sort order cannot be negative.", "Category.InvalidSortOrder");
        }

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        SortOrder = sortOrder;
    }

    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException("Category is already active.", "Category.AlreadyActive");
        }

        IsActive = true;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException("Category is already inactive.", "Category.AlreadyInactive");
        }

        IsActive = false;
    }

    public void SoftDelete(Guid? deletedBy, DateTimeOffset deletedAt)
    {
        if (IsDeleted)
        {
            throw new DomainException("Category is already deleted.", "Category.AlreadyDeleted");
        }

        MarkDeleted(deletedBy, deletedAt);
    }
}
