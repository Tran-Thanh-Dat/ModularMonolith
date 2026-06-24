using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Workspaces;

namespace Organizations.Application.Workspaces.GetWorkspaces;

public sealed record GetWorkspacesQuery(
    Guid? TenantId,
    Guid? OrganizationId,
    string? Keyword,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<WorkspaceListItemResponse>>;

public sealed class GetWorkspacesQueryValidator : AbstractValidator<GetWorkspacesQuery>
{
    public GetWorkspacesQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetWorkspacesQueryHandler : IRequestHandler<GetWorkspacesQuery, Result<PagedResult<WorkspaceListItemResponse>>>
{
    private readonly IWorkspaceService _workspaceService;

    public GetWorkspacesQueryHandler(IWorkspaceService workspaceService) => _workspaceService = workspaceService;

    public async Task<Result<PagedResult<WorkspaceListItemResponse>>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
    {
        var result = await _workspaceService.GetListAsync(
            request.TenantId,
            request.OrganizationId,
            request.Keyword,
            request.IsActive,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<WorkspaceListItemResponse>>.Success(result);
    }
}
