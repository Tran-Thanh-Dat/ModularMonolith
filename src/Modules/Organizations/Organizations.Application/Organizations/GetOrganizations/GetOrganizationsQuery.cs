using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Organizations;
using Organizations.Domain.Enums;

namespace Organizations.Application.Organizations.GetOrganizations;

public sealed record GetOrganizationsQuery(
    Guid? TenantId,
    Guid? ParentOrganizationId,
    string? Keyword,
    bool? IsActive,
    OrganizationType? Type,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<OrganizationListItemResponse>>;

public sealed class GetOrganizationsQueryValidator : AbstractValidator<GetOrganizationsQuery>
{
    public GetOrganizationsQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetOrganizationsQueryHandler : IRequestHandler<GetOrganizationsQuery, Result<PagedResult<OrganizationListItemResponse>>>
{
    private readonly IOrganizationService _organizationService;

    public GetOrganizationsQueryHandler(IOrganizationService organizationService) =>
        _organizationService = organizationService;

    public async Task<Result<PagedResult<OrganizationListItemResponse>>> Handle(GetOrganizationsQuery request, CancellationToken cancellationToken)
    {
        var result = await _organizationService.GetListAsync(
            request.TenantId,
            request.ParentOrganizationId,
            request.Keyword,
            request.IsActive,
            request.Type,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<OrganizationListItemResponse>>.Success(result);
    }
}
