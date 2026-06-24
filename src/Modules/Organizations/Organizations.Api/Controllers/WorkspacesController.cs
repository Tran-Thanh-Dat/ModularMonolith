using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organizations.Api.Contracts;
using Organizations.Application.Permissions;
using Organizations.Application.Workspaces;
using Organizations.Application.Workspaces.ActivateWorkspace;
using Organizations.Application.Workspaces.CreateWorkspace;
using Organizations.Application.Workspaces.DeactivateWorkspace;
using Organizations.Application.Workspaces.DeleteWorkspace;
using Organizations.Application.Workspaces.GetWorkspaceById;
using Organizations.Application.Workspaces.GetWorkspaces;
using Organizations.Application.Workspaces.UpdateWorkspace;

namespace Organizations.Api.Controllers;

[Authorize]
[Route("api/v1/workspaces")]
public sealed class WorkspacesController : BaseApiController
{
    private readonly ISender _sender;

    public WorkspacesController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceView)]
    public async Task<ActionResult<PagedResponse<WorkspaceListItemResponse>>> GetWorkspaces(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? organizationId,
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetWorkspacesQuery(tenantId, organizationId, keyword, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceView)]
    public async Task<ActionResult<ApiResponse<WorkspaceDetailResponse>>> GetWorkspaceById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWorkspaceByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateWorkspaceResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateWorkspaceResponse>>> CreateWorkspace(
        [FromBody] CreateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateWorkspaceCommand(
                request.TenantId,
                request.OrganizationId,
                request.Code,
                request.Name,
                request.Description,
                request.Metadata),
            cancellationToken);

        return CreatedFromResult(nameof(GetWorkspaceById), new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty }, result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> UpdateWorkspace(
        Guid id,
        [FromBody] UpdateWorkspaceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateWorkspaceCommand(id, request.Name, request.Description, request.OrganizationId, request.Metadata),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> DeleteWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteWorkspaceCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> ActivateWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateWorkspaceCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateWorkspace(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateWorkspaceCommand(id), cancellationToken);
        return FromResult(result);
    }
}
