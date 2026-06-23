using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.GetLoginPolicy;

public sealed record GetLoginPolicyQuery() : IQuery<LoginPolicyResponse>;

public sealed class GetLoginPolicyQueryHandler
    : IRequestHandler<GetLoginPolicyQuery, Result<LoginPolicyResponse>>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public GetLoginPolicyQueryHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result<LoginPolicyResponse>> Handle(
        GetLoginPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _accessPolicyService.GetLoginPolicyAsync(cancellationToken);
        return Result<LoginPolicyResponse>.Success(policy);
    }
}
