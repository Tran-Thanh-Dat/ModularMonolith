using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using Categories.Application.Abstractions;
using Categories.Application.Categories;
using FluentValidation;
using MediatR;

namespace Categories.Application.Categories.GetCategories;

public sealed record GetCategoriesQuery(
    string? Keyword,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<CategoryListItemResponse>>;

public sealed class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, Result<PagedResult<CategoryListItemResponse>>>
{
    private readonly ICategoryService _categoryService;

    public GetCategoriesQueryHandler(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<Result<PagedResult<CategoryListItemResponse>>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.GetListAsync(
            request.Keyword,
            request.IsActive,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<CategoryListItemResponse>>.Success(result);
    }
}
