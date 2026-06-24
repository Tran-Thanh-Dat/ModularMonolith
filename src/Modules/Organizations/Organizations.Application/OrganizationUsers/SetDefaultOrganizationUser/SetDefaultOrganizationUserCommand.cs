using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.OrganizationUsers.SetDefaultOrganizationUser;

public sealed record SetDefaultOrganizationUserCommand(Guid Id) : ICommand;

public sealed class SetDefaultOrganizationUserCommandValidator : AbstractValidator<SetDefaultOrganizationUserCommand>
{
    public SetDefaultOrganizationUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class SetDefaultOrganizationUserCommandHandler : IRequestHandler<SetDefaultOrganizationUserCommand, Result>
{
    private readonly IOrganizationUserService _organizationUserService;

    public SetDefaultOrganizationUserCommandHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result> Handle(SetDefaultOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        await _organizationUserService.SetDefaultAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
