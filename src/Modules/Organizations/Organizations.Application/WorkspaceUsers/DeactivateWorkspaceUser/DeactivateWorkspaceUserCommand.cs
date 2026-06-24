using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.WorkspaceUsers.DeactivateWorkspaceUser;

public sealed record DeactivateWorkspaceUserCommand(Guid Id) : ICommand;

public sealed class DeactivateWorkspaceUserCommandValidator : AbstractValidator<DeactivateWorkspaceUserCommand>
{
    public DeactivateWorkspaceUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeactivateWorkspaceUserCommandHandler : IRequestHandler<DeactivateWorkspaceUserCommand, Result>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public DeactivateWorkspaceUserCommandHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result> Handle(DeactivateWorkspaceUserCommand request, CancellationToken cancellationToken)
    {
        await _workspaceUserService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
