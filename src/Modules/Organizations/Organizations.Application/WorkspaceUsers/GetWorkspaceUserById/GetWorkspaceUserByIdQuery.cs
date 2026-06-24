using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.WorkspaceUsers;

namespace Organizations.Application.WorkspaceUsers.GetWorkspaceUserById;

public sealed record GetWorkspaceUserByIdQuery(Guid Id) : IQuery<WorkspaceUserDetailResponse>;

public sealed class GetWorkspaceUserByIdQueryValidator : AbstractValidator<GetWorkspaceUserByIdQuery>
{
    public GetWorkspaceUserByIdQueryValidator() => RuleFor(q => q.Id).NotEmpty();
}

public sealed class GetWorkspaceUserByIdQueryHandler : IRequestHandler<GetWorkspaceUserByIdQuery, Result<WorkspaceUserDetailResponse>>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public GetWorkspaceUserByIdQueryHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result<WorkspaceUserDetailResponse>> Handle(
        GetWorkspaceUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var membership = await _workspaceUserService.GetByIdAsync(request.Id, cancellationToken);
        if (membership is null)
        {
            throw new NotFoundException(
                WorkspaceUserErrors.NotFound,
                $"Workspace membership with id '{request.Id}' was not found.");
        }

        return Result<WorkspaceUserDetailResponse>.Success(membership);
    }
}
