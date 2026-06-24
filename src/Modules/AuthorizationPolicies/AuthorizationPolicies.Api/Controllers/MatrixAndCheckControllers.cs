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
[Route("api/v1/authorization-matrix")]
public sealed class AuthorizationMatrixController : BaseApiController
{
    private readonly ISender _sender;

    public AuthorizationMatrixController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixView)]
    public async Task<ActionResult<PagedResponse<AuthorizationMatrixEntryListItemResponse>>> GetList(
        [FromQuery] string? moduleCode,
        [FromQuery] string? resourceType,
        [FromQuery] string? action,
        [FromQuery] AuthorizationScope? scope,
        [FromQuery] string? requiredPermissionCode,
        [FromQuery] bool? isEnabled,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new GetAuthorizationMatrixEntriesQuery(
            moduleCode, resourceType, action, scope, requiredPermissionCode, isEnabled, pageIndex, pageSize), cancellationToken);
        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixView)]
    public async Task<ActionResult<ApiResponse<AuthorizationMatrixEntryDetailResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuthorizationMatrixEntryByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateAuthorizationMatrixEntryResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateAuthorizationMatrixEntryResponse>>> Create(
        [FromBody] CreateAuthorizationMatrixEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateAuthorizationMatrixEntryCommand(
            request.ModuleCode, request.ResourceType, request.Action, (AuthorizationScope)request.Scope,
            request.RequiredPermissionCode, request.Description, request.Metadata), cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixManage)]
    public async Task<ActionResult<ApiResponse>> Update(Guid id, [FromBody] UpdateAuthorizationMatrixEntryRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateAuthorizationMatrixEntryCommand(
            id, request.ModuleCode, request.ResourceType, request.Action, (AuthorizationScope)request.Scope,
            request.RequiredPermissionCode, request.Description, request.Metadata), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixManage)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteAuthorizationMatrixEntryCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/enable")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixManage)]
    public async Task<ActionResult<ApiResponse>> Enable(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new EnableAuthorizationMatrixEntryCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/disable")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationMatrixManage)]
    public async Task<ActionResult<ApiResponse>> Disable(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DisableAuthorizationMatrixEntryCommand(id), cancellationToken);
        return FromResult(result);
    }
}

[Authorize]
[Route("api/v1/authorization-checks")]
public sealed class AuthorizationChecksController : BaseApiController
{
    private readonly ISender _sender;

    public AuthorizationChecksController(ISender sender) => _sender = sender;

    [HttpPost("evaluate")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationCheckExecute)]
    public async Task<ActionResult<ApiResponse<AuthorizationDecisionResponse>>> Evaluate(
        [FromBody] EvaluateAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new EvaluateAuthorizationCommand(
            request.UserId,
            request.PermissionCode,
            MapContext(request.ResourceContext),
            request.Action,
            request.ResourceType,
            request.ModuleCode), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("explain")]
    [HasPermission(AuthorizationPoliciesPermissionCodes.AuthorizationCheckExplain)]
    public async Task<ActionResult<ApiResponse<AuthorizationExplanationResponse>>> Explain(
        [FromBody] ExplainAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ExplainAuthorizationCommand(
            request.UserId,
            request.PermissionCode,
            MapContext(request.ResourceContext),
            request.Action,
            request.ResourceType,
            request.ModuleCode), cancellationToken);
        return FromResult(result);
    }

    private static AuthorizationResourceContextDto MapContext(AuthorizationResourceContextRequest request) => new()
    {
        ResourceType = request.ResourceType,
        ResourceId = request.ResourceId,
        TenantId = request.TenantId,
        OrganizationId = request.OrganizationId,
        WorkspaceId = request.WorkspaceId,
        OwnerUserId = request.OwnerUserId,
        AssignedUserIds = request.AssignedUserIds,
        CreatedBy = request.CreatedBy,
        Metadata = request.Metadata
    };
}
