using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.WorkspaceUsers;

namespace Organizations.Application.WorkspaceUsers.GetWorkspaceUsers;

public sealed record GetWorkspaceUsersQuery(
    Guid? TenantId,
    Guid? WorkspaceId,
    Guid? UserId,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<WorkspaceUserListItemResponse>>;

public sealed class GetWorkspaceUsersQueryValidator : AbstractValidator<GetWorkspaceUsersQuery>
{
    public GetWorkspaceUsersQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetWorkspaceUsersQueryHandler : IRequestHandler<GetWorkspaceUsersQuery, Result<PagedResult<WorkspaceUserListItemResponse>>>
{
    private readonly IWorkspaceUserService _workspaceUserService;

    public GetWorkspaceUsersQueryHandler(IWorkspaceUserService workspaceUserService) =>
        _workspaceUserService = workspaceUserService;

    public async Task<Result<PagedResult<WorkspaceUserListItemResponse>>> Handle(GetWorkspaceUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _workspaceUserService.GetListAsync(
            request.TenantId,
            request.WorkspaceId,
            request.UserId,
            request.IsActive,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<WorkspaceUserListItemResponse>>.Success(result);
    }
}
