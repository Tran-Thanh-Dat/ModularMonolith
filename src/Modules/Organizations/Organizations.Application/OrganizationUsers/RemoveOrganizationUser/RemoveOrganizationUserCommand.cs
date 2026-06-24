using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.OrganizationUsers.RemoveOrganizationUser;

public sealed record RemoveOrganizationUserCommand(Guid Id) : ICommand;

public sealed class RemoveOrganizationUserCommandValidator : AbstractValidator<RemoveOrganizationUserCommand>
{
    public RemoveOrganizationUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class RemoveOrganizationUserCommandHandler : IRequestHandler<RemoveOrganizationUserCommand, Result>
{
    private readonly IOrganizationUserService _organizationUserService;

    public RemoveOrganizationUserCommandHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result> Handle(RemoveOrganizationUserCommand request, CancellationToken cancellationToken)
    {
        await _organizationUserService.RemoveAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
