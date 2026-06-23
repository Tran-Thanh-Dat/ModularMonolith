using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.AssignRoles;

public sealed record AssignRolesToUserCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> RoleIds) : ICommand;

public sealed class AssignRolesToUserCommandValidator : AbstractValidator<AssignRolesToUserCommand>
{
    public AssignRolesToUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.RoleIds)
            .NotEmpty()
            .Must(roleIds => roleIds.Distinct().Count() == roleIds.Count)
            .WithMessage("RoleIds must not contain duplicates.");
    }
}

public sealed class AssignRolesToUserCommandHandler : IRequestHandler<AssignRolesToUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public AssignRolesToUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(AssignRolesToUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.AssignRolesAsync(
            request.UserId,
            request.RoleIds,
            cancellationToken);

        return Result.Success();
    }
}
