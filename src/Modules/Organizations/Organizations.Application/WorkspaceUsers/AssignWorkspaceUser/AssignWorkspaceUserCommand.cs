using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.WorkspaceUsers;

namespace Organizations.Application.WorkspaceUsers.AssignWorkspaceUser;

public sealed record AssignWorkspaceUserCommand(Guid TenantId, Guid WorkspaceId, Guid UserId) : ICommand<AssignWorkspaceUserResponse>;

public sealed class AssignWorkspaceUserCommandValidator : AbstractValidator<AssignWorkspaceUserCommand>
{
    public AssignWorkspaceUserCommandValidator()
    {
        RuleFor(c => c.TenantId).NotEmpty();
        RuleFor(c => c.WorkspaceId).NotEmpty();
        RuleFor(c => c.UserId).NotEmpty();
    }
}

public sealed class AssignWorkspaceUserCommandHandler : IRequestHandler<AssignWorkspaceUserCommand, Result<AssignWorkspaceUserResponse>>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public AssignWorkspaceUserCommandHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result<AssignWorkspaceUserResponse>> Handle(AssignWorkspaceUserCommand request, CancellationToken cancellationToken)
    {
        var id = await _workspaceUserService.AssignAsync(request.TenantId, request.WorkspaceId, request.UserId, cancellationToken);
        return Result<AssignWorkspaceUserResponse>.Success(new AssignWorkspaceUserResponse { Id = id });
    }
}
