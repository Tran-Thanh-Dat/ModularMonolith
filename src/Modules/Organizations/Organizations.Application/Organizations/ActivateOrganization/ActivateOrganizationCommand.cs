using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Organizations.ActivateOrganization;

public sealed record ActivateOrganizationCommand(Guid Id) : ICommand;

public sealed class ActivateOrganizationCommandValidator : AbstractValidator<ActivateOrganizationCommand>
{
    public ActivateOrganizationCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class ActivateOrganizationCommandHandler : IRequestHandler<ActivateOrganizationCommand, Result>
{
    private readonly IOrganizationService _organizationService;

    public ActivateOrganizationCommandHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result> Handle(ActivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        await _organizationService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
