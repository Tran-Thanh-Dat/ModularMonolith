using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Organizations.DeactivateOrganization;

public sealed record DeactivateOrganizationCommand(Guid Id) : ICommand;

public sealed class DeactivateOrganizationCommandValidator : AbstractValidator<DeactivateOrganizationCommand>
{
    public DeactivateOrganizationCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeactivateOrganizationCommandHandler : IRequestHandler<DeactivateOrganizationCommand, Result>
{
    private readonly IOrganizationService _organizationService;

    public DeactivateOrganizationCommandHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        await _organizationService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
