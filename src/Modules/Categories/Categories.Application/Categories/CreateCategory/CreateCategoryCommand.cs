using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Categories.Application.Abstractions;
using Categories.Application.Categories;
using FluentValidation;
using MediatR;

namespace Categories.Application.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    string Code,
    string Name,
    string? Description,
    int SortOrder) : ICommand<CreateCategoryResponse>;

public sealed class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Description)
            .MaximumLength(1000);

        RuleFor(command => command.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CreateCategoryResponse>>
{
    private readonly ICategoryService _categoryService;

    public CreateCategoryCommandHandler(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<Result<CreateCategoryResponse>> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var id = await _categoryService.CreateAsync(
            request.Code,
            request.Name,
            request.Description,
            request.SortOrder,
            cancellationToken);

        return Result<CreateCategoryResponse>.Success(new CreateCategoryResponse { Id = id });
    }
}
