using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.DeactivateUser;

public sealed record DeactivateUserCommand(Guid Id) : ICommand;

public sealed class DeactivateUserCommandValidator : AbstractValidator<DeactivateUserCommand>
{
    public DeactivateUserCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public DeactivateUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.DeactivateUserAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
