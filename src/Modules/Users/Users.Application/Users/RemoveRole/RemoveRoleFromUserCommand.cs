using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.RemoveRole;

public sealed record RemoveRoleFromUserCommand(
    Guid UserId,
    Guid RoleId) : ICommand;

public sealed class RemoveRoleFromUserCommandValidator : AbstractValidator<RemoveRoleFromUserCommand>
{
    public RemoveRoleFromUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.RoleId).NotEmpty();
    }
}

public sealed class RemoveRoleFromUserCommandHandler : IRequestHandler<RemoveRoleFromUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public RemoveRoleFromUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(RemoveRoleFromUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.RemoveRoleAsync(
            request.UserId,
            request.RoleId,
            cancellationToken);

        return Result.Success();
    }
}
