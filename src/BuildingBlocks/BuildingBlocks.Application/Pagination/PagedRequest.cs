namespace BuildingBlocks.Application.Pagination;

public class PagedRequest
{
    private const int DefaultPageIndex = 1;
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public int PageIndex { get; init; } = DefaultPageIndex;

    public int PageNumber
    {
        get => PageIndex;
        init => PageIndex = value;
    }

    public int PageSize { get; init; } = DefaultPageSize;

    public string? Keyword { get; init; }

    public string? SortBy { get; init; }

    public string? SortDirection { get; init; }

    public int NormalizedPageIndex => PageIndex < 1 ? DefaultPageIndex : PageIndex;

    public int NormalizedPageNumber => NormalizedPageIndex;

    public int NormalizedPageSize => PageSize switch
    {
        < 1 => DefaultPageSize,
        > MaxPageSize => MaxPageSize,
        _ => PageSize
    };

    public int Skip => (NormalizedPageIndex - 1) * NormalizedPageSize;

    public int Take => NormalizedPageSize;

    public bool IsDescending =>
        string.Equals(SortDirection, "desc", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(SortDirection, "descending", StringComparison.OrdinalIgnoreCase);
}
