using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using Categories.Application.Abstractions;
using FluentValidation;
using MediatR;

namespace Categories.Application.Categories.DeactivateCategory;

public sealed record DeactivateCategoryCommand(Guid Id) : ICommand;

public sealed class DeactivateCategoryCommandValidator : AbstractValidator<DeactivateCategoryCommand>
{
    public DeactivateCategoryCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeactivateCategoryCommandHandler : IRequestHandler<DeactivateCategoryCommand, Result>
{
    private readonly ICategoryService _categoryService;

    public DeactivateCategoryCommandHandler(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    public async Task<Result> Handle(DeactivateCategoryCommand request, CancellationToken cancellationToken)
    {
        await _categoryService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
