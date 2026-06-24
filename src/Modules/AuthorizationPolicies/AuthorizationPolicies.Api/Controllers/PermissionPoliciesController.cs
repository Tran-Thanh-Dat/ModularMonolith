using AuthorizationPolicies.Api.Contracts;
using AuthorizationPolicies.Application.AuthorizationChecks;
using AuthorizationPolicies.Application.AuthorizationMatrix;
using AuthorizationPolicies.Application.PermissionPolicies;
using AuthorizationPolicies.Application.Permissions;
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
[Route("api/v1/permission-policies")]
public sealed class PermissionPoliciesController : BaseApiController
{
    private readonly ISender _sender;

    public PermissionPoliciesController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyView)]
    public async Task<ActionResult<PagedResponse<PermissionPolicyListItemResponse>>> GetList(
        [FromQuery] string? keyword,
        [FromQuery] string? permissionCode,
        [FromQuery] string? moduleCode,
        [FromQuery] string? resourceType,
        [FromQuery] string? action,
        [FromQuery] AuthorizationScope? scope,
        [FromQuery] AuthorizationEffect? effect,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetPermissionPoliciesQuery(
            keyword, permissionCode, moduleCode, resourceType, action, scope, effect, isActive, pageIndex, pageSize), cancellationToken);
        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyView)]
    public async Task<ActionResult<ApiResponse<PermissionPolicyDetailResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetPermissionPolicyByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    [ProducesResponseType(typeof(ApiResponse<CreatePermissionPolicyResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreatePermissionPolicyResponse>>> Create(
        [FromBody] CreatePermissionPolicyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreatePermissionPolicyCommand(
            request.Code, request.Name, request.Description, request.PermissionCode, request.ModuleCode,
            request.ResourceType, request.Action, (AuthorizationScope)request.Scope, (AuthorizationEffect)request.Effect,
            request.Priority, request.Conditions), cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdatePermissionPolicyRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdatePermissionPolicyCommand(
            id, request.Name, request.Description, request.PermissionCode, request.ModuleCode,
            request.ResourceType, request.Action, (AuthorizationScope)request.Scope, (AuthorizationEffect)request.Effect,
            request.Priority, request.Conditions), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeletePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivatePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.PermissionPolicyManage)]
    public async Task<ActionResult<ApiResponse>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivatePermissionPolicyCommand(id), cancellationToken);
        return FromResult(result);
    }
}
