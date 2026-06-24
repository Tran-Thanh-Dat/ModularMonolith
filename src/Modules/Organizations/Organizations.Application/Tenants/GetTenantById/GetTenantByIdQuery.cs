using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Organizations.Application.Abstractions;
using Organizations.Application.Tenants;

namespace Organizations.Application.Tenants.GetTenantById;

public sealed record GetTenantByIdQuery(Guid Id) : IQuery<TenantDetailResponse>;

public sealed class GetTenantByIdQueryValidator : AbstractValidator<GetTenantByIdQuery>
{
    public GetTenantByIdQueryValidator() => RuleFor(q => q.Id).NotEmpty();
}

public sealed class GetTenantByIdQueryHandler : IRequestHandler<GetTenantByIdQuery, Result<TenantDetailResponse>>
{
    private readonly ITenantService _tenantService;

    public GetTenantByIdQueryHandler(ITenantService tenantService) => _tenantService = tenantService;

    public async Task<Result<TenantDetailResponse>> Handle(GetTenantByIdQuery request, CancellationToken cancellationToken)
    {
        var tenant = await _tenantService.GetByIdAsync(request.Id, cancellationToken);
        if (tenant is null)
        {
            throw new NotFoundException(TenantErrors.NotFound, $"Tenant with id '{request.Id}' was not found.");
        }

        return Result<TenantDetailResponse>.Success(tenant);
    }
}
