using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.OrganizationUsers;

namespace Organizations.Application.OrganizationUsers.GetOrganizationUsers;

public sealed record GetOrganizationUsersQuery(
    Guid? TenantId,
    Guid? OrganizationId,
    Guid? UserId,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<OrganizationUserListItemResponse>>;

public sealed class GetOrganizationUsersQueryValidator : AbstractValidator<GetOrganizationUsersQuery>
{
    public GetOrganizationUsersQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetOrganizationUsersQueryHandler : IRequestHandler<GetOrganizationUsersQuery, Result<PagedResult<OrganizationUserListItemResponse>>>
{
    private readonly IOrganizationUserService _organizationUserService;

    public GetOrganizationUsersQueryHandler(IOrganizationUserService organizationUserService) =>
        _organizationUserService = organizationUserService;

    public async Task<Result<PagedResult<OrganizationUserListItemResponse>>> Handle(GetOrganizationUsersQuery request, CancellationToken cancellationToken)
    {
        var result = await _organizationUserService.GetListAsync(
            request.TenantId,
            request.OrganizationId,
            request.UserId,
            request.IsActive,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<OrganizationUserListItemResponse>>.Success(result);
    }
}
