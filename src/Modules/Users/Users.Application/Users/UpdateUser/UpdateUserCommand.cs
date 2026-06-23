using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid Id,
    string Email,
    string FullName) : ICommand;

public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(command => command.Id)
            .NotEmpty();

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(255);
    }
}

public sealed class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserManagementService _userManagementService;

    public UpdateUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        await _userManagementService.UpdateUserAsync(
            request.Id,
            request.Email,
            request.FullName,
            cancellationToken);

        return Result.Success();
    }
}
