namespace Categories.Api.Contracts;

public sealed class CreateCategoryRequest
{
    public string Code { get; init; } = default!;

    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public int SortOrder { get; init; }
}

/// <summary>
/// Update payload for a category. Code is immutable after creation; only name, description, and sort order can change.
/// </summary>
public sealed class UpdateCategoryRequest
{
    public string Name { get; init; } = default!;

    public string? Description { get; init; }

    public int SortOrder { get; init; }
}
