using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using Categories.Application.Abstractions;
using Categories.Application.Categories;
using FluentValidation;
using MediatR;

namespace Categories.Application.Categories.GetCategoryById;

public sealed record GetCategoryByIdQuery(Guid Id) : IQuery<CategoryDetailResponse>;

public sealed class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, Result<CategoryDetailResponse>>
{
    private readonly ICategoryService _categoryService;

    public GetCategoryByIdQueryHandler(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<Result<CategoryDetailResponse>> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var category = await _categoryService.GetByIdAsync(request.Id, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                CategoryErrors.NotFound,
                $"Category with id '{request.Id}' was not found.");
        }

        return Result<CategoryDetailResponse>.Success(category);
    }
}
