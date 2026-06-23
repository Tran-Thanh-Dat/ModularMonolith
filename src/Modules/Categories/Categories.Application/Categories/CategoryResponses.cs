namespace Categories.Application.Categories;

public sealed class CategoryListItemResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CategoryDetailResponse
{
    public Guid Id { get; init; }

    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public Guid? CreatedBy { get; init; }

    public DateTimeOffset? UpdatedAt { get; init; }

    public Guid? UpdatedBy { get; init; }
}

public sealed class CreateCategoryResponse
{
    public Guid Id { get; init; }
}
