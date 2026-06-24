using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Workspaces;

namespace Organizations.Application.Workspaces.CreateWorkspace;

public sealed record CreateWorkspaceCommand(
    Guid TenantId,
    Guid? OrganizationId,
    string Code,
    string Name,
    string? Description,
    string? Metadata) : ICommand<CreateWorkspaceResponse>;

public sealed class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public CreateWorkspaceCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.Code).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Code is required.").MaximumLength(100);
        RuleFor(c => c.Name).Must(c => !string.IsNullOrWhiteSpace(c)).WithMessage("Name is required.").MaximumLength(255);
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Metadata).MaximumLength(4000).When(c => c.Metadata is not null);
    }
}

public sealed class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result<CreateWorkspaceResponse>>
{
    private readonly IWorkspaceService _workspaceService;

    public CreateWorkspaceCommandHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result<CreateWorkspaceResponse>> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
    {
        var id = await _workspaceService.CreateAsync(
            request.TenantId,
            request.OrganizationId,
            request.Code,
            request.Name,
            request.Description,
            request.Metadata,
            cancellationToken);

        return Result<CreateWorkspaceResponse>.Success(new CreateWorkspaceResponse { Id = id });
    }
}
