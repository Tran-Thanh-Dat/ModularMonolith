using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Settings.Application.Abstractions;
using Settings.Application.AccessPolicy;

namespace Settings.Application.AccessPolicy.GetPasswordPolicy;

public sealed record GetPasswordPolicyQuery() : IQuery<PasswordPolicyResponse>;

public sealed class GetPasswordPolicyQueryHandler
    : IRequestHandler<GetPasswordPolicyQuery, Result<PasswordPolicyResponse>>
{
    private readonly IAccessPolicyService _accessPolicyService;

    public GetPasswordPolicyQueryHandler(IAccessPolicyService accessPolicyService)
    {
        _accessPolicyService = accessPolicyService;
    }

    public async Task<Result<PasswordPolicyResponse>> Handle(
        GetPasswordPolicyQuery request,
        CancellationToken cancellationToken)
    {
        var policy = await _accessPolicyService.GetPasswordPolicyAsync(cancellationToken);
        return Result<PasswordPolicyResponse>.Success(policy);
    }
}
