using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Organizations.DeleteOrganization;

public sealed record DeleteOrganizationCommand(Guid Id) : ICommand;

public sealed class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
{
    public DeleteOrganizationCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand, Result>
{
    private readonly IOrganizationService _organizationService;

    public DeleteOrganizationCommandHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result> Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
    {
        await _organizationService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
