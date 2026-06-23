using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.AssignPermissions;

public sealed record AssignPermissionsToUserCommand(
    Guid UserId,
    IReadOnlyCollection<Guid> PermissionIds) : ICommand;

public sealed class AssignPermissionsToUserCommandValidator : AbstractValidator<AssignPermissionsToUserCommand>
{
    public AssignPermissionsToUserCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty();

        RuleFor(command => command.PermissionIds)
            .NotEmpty()
            .Must(permissionIds => permissionIds.Distinct().Count() == permissionIds.Count)
            .WithMessage("PermissionIds must not contain duplicates.");
    }
}

public sealed class AssignPermissionsToUserCommandHandler : IRequestHandler<AssignPermissionsToUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public AssignPermissionsToUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(AssignPermissionsToUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.AssignPermissionsAsync(
            request.UserId,
            request.PermissionIds,
            cancellationToken);

        return Result.Success();
    }
}
