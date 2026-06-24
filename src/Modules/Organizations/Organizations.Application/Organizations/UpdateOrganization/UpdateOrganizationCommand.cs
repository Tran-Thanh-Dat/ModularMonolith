using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Domain.Enums;

namespace Organizations.Application.Organizations.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    Guid Id,
    string Name,
    string? Description,
    OrganizationType Type,
    int SortOrder,
    string? Metadata,
    Guid? ParentOrganizationId) : ICommand;

public sealed class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
{
    public UpdateOrganizationCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.SortOrder).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
        RuleFor(c => c).Must(c => c.ParentOrganizationId != c.Id)
            .WithMessage("Organization cannot be its own parent.");
    }
}

public sealed class UpdateOrganizationCommandHandler : IRequestHandler<UpdateOrganizationCommand, Result>
{
    private readonly IOrganizationService _organizationService;

    public UpdateOrganizationCommandHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result> Handle(UpdateOrganizationCommand request, CancellationToken cancellationToken)
    {
        await _organizationService.UpdateAsync(
            request.Id,
            request.Name,
            request.Description,
            request.Type,
            request.SortOrder,
            request.Metadata,
            request.ParentOrganizationId,
            cancellationToken);

        return Result.Success();
    }
}
