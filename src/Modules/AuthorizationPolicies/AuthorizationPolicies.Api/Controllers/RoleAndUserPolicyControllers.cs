using AuthorizationPolicies.Api.Contracts;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Application.Permissions;
using AuthorizationPolicies.Application.RolePermissionPolicies;
using AuthorizationPolicies.Application.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Domain.Enums;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AuthorizationPolicies.Api.Controllers;

[Authorize]
[Route("api/v1/permission-policies/role-assignments")]
public sealed class RolePermissionPoliciesController : BaseApiController
{
    private readonly ISender _sender;

    public RolePermissionPoliciesController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyView)]
    public async Task<ActionResult<PagedResponse<RolePermissionPolicyListItemResponse>>> GetList(
        [FromQuery] Guid? roleId,
        [FromQuery] Guid? permissionPolicyId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetRolePermissionPoliciesQuery(roleId, permissionPolicyId, isActive, pageIndex, pageSize), cancellationToken);
        return FromPagedResult(result);
    }

    [HttpPost]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<AssignRolePermissionPolicyResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AssignRolePermissionPolicyResponse>>> Assign(
        [FromBody] AssignRolePermissionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignRolePermissionPolicyCommand(request.RoleId, request.PermissionPolicyId), cancellationToken);
        return CreatedFromResult(string.Empty, null, result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Remove(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveRolePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateRolePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateRolePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }
}

[Authorize]
[Route("api/v1/permission-policies/user-overrides")]
public sealed class UserPermissionPolicyOverridesController : BaseApiController
{
    private readonly ISender _sender;

    public UserPermissionPolicyOverridesController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyView)]
    public async Task<ActionResult<PagedResponse<UserPermissionPolicyOverrideListItemResponse>>> GetList(
        [FromQuery] Guid? userId,
        [FromQuery] Guid? permissionPolicyId,
        [FromQuery] AuthorizationEffect? effect,
        [FromQuery] bool? isActive,
        [FromQuery] bool includeExpired = false,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetUserPermissionPolicyOverridesQuery(
            userId, permissionPolicyId, effect, isActive, includeExpired, pageIndex, pageSize), cancellationToken);
        return FromPagedResult(result);
    }

    [HttpPost]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserPermissionPolicyOverrideResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateUserPermissionPolicyOverrideResponse>>> Create(
        [FromBody] CreateUserPermissionPolicyOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateUserPermissionPolicyOverrideCommand(
            request.UserId, request.PermissionPolicyId, (AuthorizationEffect)request.Effect, request.ExpiresAt, request.Reason), cancellationToken);
        return CreatedFromResult(string.Empty, null, result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Remove(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveUserPermissionPolicyOverrideCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateUserPermissionPolicyOverrideCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateUserPermissionPolicyOverrideCommand(id), cancellationToken);
        return FromResult(result);
    }
}
