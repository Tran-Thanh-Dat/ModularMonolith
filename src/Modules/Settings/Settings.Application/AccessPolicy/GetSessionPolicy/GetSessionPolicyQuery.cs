using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.GetSessionPolicy;

public sealed record GetSessionPolicyQuery() : IQuery<SessionPolicyResponse>;

public sealed class GetSessionPolicyQueryHandler
    : IRequestHandler<GetSessionPolicyQuery, Result<SessionPolicyResponse>>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public GetSessionPolicyQueryHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result<SessionPolicyResponse>> Handle(
        GetSessionPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _accessPolicyService.GetSessionPolicyAsync(cancellationToken);
        return Result<SessionPolicyResponse>.Success(policy);
    }
}
