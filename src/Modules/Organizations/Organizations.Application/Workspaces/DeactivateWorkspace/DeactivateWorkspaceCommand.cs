using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Workspaces.DeactivateWorkspace;

public sealed record DeactivateWorkspaceCommand(Guid Id) : ICommand;

public sealed class DeactivateWorkspaceCommandValidator : AbstractValidator<DeactivateWorkspaceCommand>
{
    public DeactivateWorkspaceCommandValidator() => RuleFor(c => c.Id).NotEmpty();
}

public sealed class DeactivateWorkspaceCommandHandler : IRequestHandler<DeactivateWorkspaceCommand, Result>
{
    private readonly IWorkspaceService _workspaceService;

    public DeactivateWorkspaceCommandHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result> Handle(DeactivateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await _workspaceService.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
