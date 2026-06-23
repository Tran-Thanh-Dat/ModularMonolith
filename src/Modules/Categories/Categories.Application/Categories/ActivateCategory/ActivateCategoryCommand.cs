using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Categories.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Categories.Application.Categories.ActivateCategory;

public sealed record ActivateCategoryCommand(Guid Id) : ICommand;

public sealed class ActivateCategoryCommandValidator : AbstractValidator<ActivateCategoryCommand>
{
    public ActivateCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class ActivateCategoryCommandHandler : IRequestHandler<ActivateCategoryCommand, Result>
{
    private readonly ICategoryService _categoryService;

    public ActivateCategoryCommandHandler(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<Result> Handle(ActivateCategoryCommand request, CancellationToken cancellationToken)
    {
        await _categoryService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
