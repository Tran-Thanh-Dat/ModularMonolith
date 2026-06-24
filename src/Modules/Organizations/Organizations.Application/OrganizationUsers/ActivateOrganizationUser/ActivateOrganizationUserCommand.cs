using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.OrganizationUsers.ActivateOrganizationUser;

public sealed record ActivateOrganizationUserCommand(Guid Id) : ICommand;

public sealed class ActivateOrganizationUserCommandValidator : AbstractValidator<ActivateOrganizationUserCommand>
{
    public ActivateOrganizationUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class ActivateOrganizationUserCommandHandler : IRequestHandler<ActivateOrganizationUserCommand, Result>
{
    private readonly IOrganizationUserService _organizationUserService;

    public ActivateOrganizationUserCommandHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result> Handle(ActivateOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        await _organizationUserService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
