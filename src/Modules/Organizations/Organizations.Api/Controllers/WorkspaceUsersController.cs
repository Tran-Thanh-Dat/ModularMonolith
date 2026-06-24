using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organizations.Api.Contracts;
using Organizations.Application.Permissions;
using Organizations.Application.WorkspaceUsers;
using Organizations.Application.WorkspaceUsers.ActivateWorkspaceUser;
using Organizations.Application.WorkspaceUsers.AssignWorkspaceUser;
using Organizations.Application.WorkspaceUsers.DeactivateWorkspaceUser;
using Organizations.Application.WorkspaceUsers.GetWorkspaceUserById;
using Organizations.Application.WorkspaceUsers.GetWorkspaceUsers;
using Organizations.Application.WorkspaceUsers.RemoveWorkspaceUser;

namespace Organizations.Api.Controllers;

[Authorize]
[Route("api/v1/workspace-users")]
public sealed class WorkspaceUsersController : BaseApiController
{
    private readonly ISender _sender;

    public WorkspaceUsersController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceView)]
    public async Task<ActionResult<PagedResponse<WorkspaceUserListItemResponse>>> GetWorkspaceUsers(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? workspaceId,
        [FromQuery] Guid? userId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetWorkspaceUsersQuery(tenantId, workspaceId, userId, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceView)]
    public async Task<ActionResult<ApiResponse<WorkspaceUserDetailResponse>>> GetWorkspaceUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetWorkspaceUserByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    [ProducesResponseType(typeof(ApiResponse<AssignWorkspaceUserResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AssignWorkspaceUserResponse>>> AssignWorkspaceUser(
        [FromBody] AssignWorkspaceUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AssignWorkspaceUserCommand(request.TenantId, request.WorkspaceId, request.UserId),
            cancellationToken);

        return CreatedFromResult(string.Empty, null, result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> ActivateWorkspaceUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateWorkspaceUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateWorkspaceUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateWorkspaceUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.WorkspaceManage)]
    public async Task<ActionResult<ApiResponse>> RemoveWorkspaceUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveWorkspaceUserCommand(id), cancellationToken);
        return FromResult(result);
    }
}
