using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuthorizationPolicies.Application.RolePermissionPolicies;

public sealed record AssignRolePermissionPolicyCommand(Guid RoleId, Guid PermissionPolicyId) : ICommand<AssignRolePermissionPolicyResponse>;

public sealed class AssignRolePermissionPolicyCommandValidator : AbstractValidator<AssignRolePermissionPolicyCommand>
{
    public AssignRolePermissionPolicyCommandValidator()
    {
        RuleFor(c => c.RoleId).NotEmpty();
        RuleFor(c => c.PermissionPolicyId).NotEmpty();
    }
}

public sealed class AssignRolePermissionPolicyCommandHandler : IRequestHandler<AssignRolePermissionPolicyCommand, Result<AssignRolePermissionPolicyResponse>>
{
    private readonly IRolePermissionPolicyService _service;

    public AssignRolePermissionPolicyCommandHandler(IRolePermissionPolicyService service) => _service = service;

    public async Task<Result<AssignRolePermissionPolicyResponse>> Handle(AssignRolePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        var id = await _service.AssignAsync(request.RoleId, request.PermissionPolicyId, cancellationToken);
        return Result<AssignRolePermissionPolicyResponse>.Success(new AssignRolePermissionPolicyResponse { Id = id });
    }
}

public sealed record GetRolePermissionPoliciesQuery(
    Guid? RoleId,
    Guid? PermissionPolicyId,
    bool? IsActive,
    int PageIndex,
    int PageSize) : IQuery<PagedResult<RolePermissionPolicyListItemResponse>>;

public sealed class GetRolePermissionPoliciesQueryHandler : IRequestHandler<GetRolePermissionPoliciesQuery, Result<PagedResult<RolePermissionPolicyListItemResponse>>>
{
    private readonly IRolePermissionPolicyService _service;

    public GetRolePermissionPoliciesQueryHandler(IRolePermissionPolicyService service) => _service = service;

    public async Task<Result<PagedResult<RolePermissionPolicyListItemResponse>>> Handle(GetRolePermissionPoliciesQuery request, CancellationToken cancellationToken)
    {
        var data = await _service.GetListAsync(request.RoleId, request.PermissionPolicyId, request.IsActive, request.PageIndex, request.PageSize, cancellationToken);
        return Result<PagedResult<RolePermissionPolicyListItemResponse>>.Success(data);
    }
}

public sealed record RemoveRolePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class RemoveRolePermissionPolicyCommandHandler : IRequestHandler<RemoveRolePermissionPolicyCommand, Result>
{
    private readonly IRolePermissionPolicyService _service;

    public RemoveRolePermissionPolicyCommandHandler(IRolePermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(RemoveRolePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.RemoveAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record ActivateRolePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class ActivateRolePermissionPolicyCommandHandler : IRequestHandler<ActivateRolePermissionPolicyCommand, Result>
{
    private readonly IRolePermissionPolicyService _service;

    public ActivateRolePermissionPolicyCommandHandler(IRolePermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(ActivateRolePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.ActivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}

public sealed record DeactivateRolePermissionPolicyCommand(Guid Id) : ICommand;

public sealed class DeactivateRolePermissionPolicyCommandHandler : IRequestHandler<DeactivateRolePermissionPolicyCommand, Result>
{
    private readonly IRolePermissionPolicyService _service;

    public DeactivateRolePermissionPolicyCommandHandler(IRolePermissionPolicyService service) => _service = service;

    public async Task<Result> Handle(DeactivateRolePermissionPolicyCommand request, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(request.Id, cancellationToken);
        return Result.Success();
    }
}
