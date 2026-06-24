using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.WorkspaceUsers.ActivateWorkspaceUser;

public sealed record ActivateWorkspaceUserCommand(Guid Id) : ICommand;

public sealed class ActivateWorkspaceUserCommandValidator : AbstractValidator<ActivateWorkspaceUserCommand>
{
    public ActivateWorkspaceUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class ActivateWorkspaceUserCommandHandler : IRequestHandler<ActivateWorkspaceUserCommand, Result>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public ActivateWorkspaceUserCommandHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result> Handle(ActivateWorkspaceUserCommand request, CancellationToken cancellationToken)
    {
        await _workspaceUserService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
