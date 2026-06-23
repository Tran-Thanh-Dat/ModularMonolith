using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Users.Application.Abstractions;

namespace Users.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string UserName,
    string Email,
    string FullName,
    string Password,
    IReadOnlyCollection<Guid> RoleIds) : ICommand<CreateUserResponse>;

public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(command => command.UserName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(command => command.FullName)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

        RuleFor(command => command.RoleIds)
            .NotNull();
    }
}

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<CreateUserResponse>>
{
    private readonly IUserManagementService _userManagementService;

    public CreateUserCommandHandler(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var userId = await _userManagementService.CreateUserAsync(
            request.UserName,
            request.Email,
            request.FullName,
            request.Password,
            request.RoleIds,
            cancellationToken);

        return Result<CreateUserResponse>.Success(new CreateUserResponse { Id = userId });
    }
}
