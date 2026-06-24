using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Workspaces;

namespace Organizations.Application.Workspaces.GetWorkspaceById;

public sealed record GetWorkspaceByIdQuery(Guid Id) : IQuery<WorkspaceDetailResponse>;

public sealed class GetWorkspaceByIdQueryValidator : AbstractValidator<GetWorkspaceByIdQuery>
{
    public GetWorkspaceByIdQueryValidator() => RuleFor(q => q.Id).NotEmpty();
}

public sealed class GetWorkspaceByIdQueryHandler : IRequestHandler<GetWorkspaceByIdQuery, Result<WorkspaceDetailResponse>>
{
    private readonly IWorkspaceService _workspaceService;

    public GetWorkspaceByIdQueryHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result<WorkspaceDetailResponse>> Handle(GetWorkspaceByIdQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceService.GetByIdAsync(request.Id, cancellationToken);
        if (workspace is null)
        {
            throw new NotFoundException(WorkspaceErrors.NotFound, $"Workspace with id '{request.Id}' was not found.");
        }

        return Result<WorkspaceDetailResponse>.Success(workspace);
    }
}
