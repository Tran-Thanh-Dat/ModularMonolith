using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Tenants;

namespace Organizations.Application.Tenants.GetTenants;

public sealed record GetTenantsQuery(
    string? Keyword,
    bool? IsActive,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<TenantListItemResponse>>;

public sealed class GetTenantsQueryValidator : AbstractValidator<GetTenantsQuery>
{
    public GetTenantsQueryValidator()
    {
        RuleFor(q => q.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetTenantsQueryHandler : IRequestHandler<GetTenantsQuery, Result<PagedResult<TenantListItemResponse>>>
{
    private readonly ITenantService _tenantService;

    public GetTenantsQueryHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result<PagedResult<TenantListItemResponse>>> Handle(GetTenantsQuery request, CancellationToken cancellationToken)
    {
        var result = await _tenantService.GetListAsync(request.Keyword, request.IsActive, request.PageIndex, request.PageSize, cancellationToken);
        return Result<PagedResult<TenantListItemResponse>>.Success(result);
    }
}
