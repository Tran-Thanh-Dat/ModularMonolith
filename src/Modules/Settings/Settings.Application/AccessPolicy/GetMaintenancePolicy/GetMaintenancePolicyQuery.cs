using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.GetMaintenancePolicy;

public sealed record GetMaintenancePolicyQuery() : IQuery<MaintenancePolicyResponse>;

public sealed class GetMaintenancePolicyQueryHandler
    : IRequestHandler<GetMaintenancePolicyQuery, Result<MaintenancePolicyResponse>>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public GetMaintenancePolicyQueryHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result<MaintenancePolicyResponse>> Handle(
        GetMaintenancePolicyQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _accessPolicyService.GetMaintenancePolicyAsync(cancellationToken);
        return Result<MaintenancePolicyResponse>.Success(policy);
    }
}
