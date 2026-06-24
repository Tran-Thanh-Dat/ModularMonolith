using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;

namespace Organizations.Application.Workspaces.UpdateWorkspace;

public sealed record UpdateWorkspaceCommand(
    Guid Id,
    string Name,
    string? Description,
    Guid? OrganizationId,
    string? Metadata) : ICommand;

public sealed class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public UpdateWorkspaceCommandValidator()
    {
        RuleFor(c => c.Id).NotEmpty();
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
    }
}

public sealed class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, Result>
{
    private readonly IWorkspaceService _workspaceService;

    public UpdateWorkspaceCommandHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        await _workspaceService.UpdateAsync(request.Id, request.Name, request.Description, request.OrganizationId, request.Metadata, cancellationToken);
        return Result.Success();
    }
}
