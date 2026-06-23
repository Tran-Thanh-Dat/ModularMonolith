using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.RemovePermission;

public sealed record RemovePermissionFromUserCommand(
    Guid UserId,
    Guid PermissionId) : ICommand;

public sealed class RemovePermissionFromUserCommandValidator : AbstractValidator<RemovePermissionFromUserCommand>
{
    public RemovePermissionFromUserCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.PermissionId).NotEmpty();
    }
}

public sealed class RemovePermissionFromUserCommandHandler : IRequestHandler<RemovePermissionFromUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public RemovePermissionFromUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(RemovePermissionFromUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.RemovePermissionAsync(
            request.UserId,
            request.PermissionId,
            cancellationToken);

        return Result.Success();
    }
}
