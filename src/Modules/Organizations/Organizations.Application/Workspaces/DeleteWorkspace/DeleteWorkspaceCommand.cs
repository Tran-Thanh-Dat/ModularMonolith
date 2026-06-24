using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Workspaces.DeleteWorkspace;

public sealed record DeleteWorkspaceCommand(Guid Id) : ICommand;

public sealed class DeleteWorkspaceCommandValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public DeleteWorkspaceCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, Result>
{
    private readonly IWorkspaceService _workspaceService;

    public DeleteWorkspaceCommandHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await _workspaceService.DeleteAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
