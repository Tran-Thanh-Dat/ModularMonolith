using BuildingBlocks.Application.Pagination;
using Categories.Application.Categories;

namespace Categories.Application.Abstractions;

public interface ICategoryService
{
    Task<Guid> CreateAsync(
        string code,
        string name,
        string? description,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        int sortOrder,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<CategoryDetailResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<CategoryListItemResponse>> GetListAsync(
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}
