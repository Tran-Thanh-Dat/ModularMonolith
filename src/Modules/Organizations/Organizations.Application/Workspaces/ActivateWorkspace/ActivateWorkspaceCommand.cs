using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Workspaces.ActivateWorkspace;

public sealed record ActivateWorkspaceCommand(Guid Id) : ICommand;

public sealed class ActivateWorkspaceCommandValidator : AbstractValidator<ActivateWorkspaceCommand>
{
    public ActivateWorkspaceCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class ActivateWorkspaceCommandHandler : IRequestHandler<ActivateWorkspaceCommand, Result>
{
    private readonly IWorkspaceService _workspaceService;

    public ActivateWorkspaceCommandHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result> Handle(ActivateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await _workspaceService.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
