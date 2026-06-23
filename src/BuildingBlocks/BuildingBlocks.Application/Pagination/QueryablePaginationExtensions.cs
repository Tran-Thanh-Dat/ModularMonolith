using BuildingBlocks.Application.Results;
using Microsoft.EntityFrameworkCore;

namespace BuildingBlocks.Application.Pagination;

public static class QueryablePaginationExtensions
{
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var request = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        return await query.ToPagedResultAsync(request, cancellationToken);
    }

    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PagedRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageIndex = request.NormalizedPageIndex;
        var pageSize = request.NormalizedPageSize;

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(request.Skip)
            .Take(request.Take)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, pageIndex, pageSize, totalCount);
    }
}
