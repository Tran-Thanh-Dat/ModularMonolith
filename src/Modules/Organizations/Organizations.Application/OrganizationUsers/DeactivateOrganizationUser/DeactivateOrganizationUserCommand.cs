using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.OrganizationUsers.DeactivateOrganizationUser;

public sealed record DeactivateOrganizationUserCommand(Guid Id) : ICommand;

public sealed class DeactivateOrganizationUserCommandValidator : AbstractValidator<DeactivateOrganizationUserCommand>
{
    public DeactivateOrganizationUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeactivateOrganizationUserCommandHandler : IRequestHandler<DeactivateOrganizationUserCommand, Result>
{
    private readonly IOrganizationUserService _organizationUserService;

    public DeactivateOrganizationUserCommandHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result> Handle(DeactivateOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        await _organizationUserService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
