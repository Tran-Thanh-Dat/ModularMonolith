using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.WorkspaceUsers.RemoveWorkspaceUser;

public sealed record RemoveWorkspaceUserCommand(Guid Id) : ICommand;

public sealed class RemoveWorkspaceUserCommandValidator : AbstractValidator<RemoveWorkspaceUserCommand>
{
    public RemoveWorkspaceUserCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class RemoveWorkspaceUserCommandHandler : IRequestHandler<RemoveWorkspaceUserCommand, Result>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public RemoveWorkspaceUserCommandHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result> Handle(RemoveWorkspaceUserCommand request, CancellationToken cancellationToken)
    {
        await _workspaceUserService.RemoveAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
