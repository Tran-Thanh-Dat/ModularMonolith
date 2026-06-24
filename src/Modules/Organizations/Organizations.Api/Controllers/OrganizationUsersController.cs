using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Organizations.Api.Contracts;
using Organizations.Application.OrganizationUsers;
using Organizations.Application.OrganizationUsers.ActivateOrganizationUser;
using Organizations.Application.OrganizationUsers.AssignOrganizationUser;
using Organizations.Application.OrganizationUsers.DeactivateOrganizationUser;
using Organizations.Application.OrganizationUsers.GetOrganizationUserById;
using Organizations.Application.OrganizationUsers.GetOrganizationUsers;
using Organizations.Application.OrganizationUsers.GetUserOrganizations;
using Organizations.Application.OrganizationUsers.RemoveOrganizationUser;
using Organizations.Application.OrganizationUsers.SetDefaultOrganizationUser;
using Organizations.Application.Permissions;

namespace Organizations.Api.Controllers;

[Authorize]
[Route("api/v1/organization-users")]
public sealed class OrganizationUsersController : BaseApiController
{
    private readonly ISender _sender;

    public OrganizationUsersController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<PagedResponse<OrganizationUserListItemResponse>>> GetOrganizationUsers(
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? organizationId,
        [FromQuery] Guid? userId,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetOrganizationUsersQuery(tenantId, organizationId, userId, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<ApiResponse<OrganizationUserDetailResponse>>> GetOrganizationUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetOrganizationUserByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("users/{userId:guid}/organizations")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationView)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<OrganizationUserListItemResponse>>>> GetUserOrganizations(
        Guid userId,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserOrganizationsQuery(userId, tenantId), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    [ProducesResponseType(typeof(ApiResponse<AssignOrganizationUserResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AssignOrganizationUserResponse>>> AssignOrganizationUser(
        [FromBody] AssignOrganizationUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AssignOrganizationUserCommand(request.TenantId, request.OrganizationId, request.UserId, request.IsDefault),
            cancellationToken);

        return CreatedFromResult(string.Empty, null, result);
    }

    [HttpPatch("{id:guid}/set-default")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> SetDefaultOrganizationUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetDefaultOrganizationUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> ActivateOrganizationUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateOrganizationUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateOrganizationUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateOrganizationUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(OrganizationsPermissionCodes.OrganizationManage)]
    public async Task<ActionResult<ApiResponse>> RemoveOrganizationUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveOrganizationUserCommand(id), cancellationToken);
        return FromResult(result);
    }
}
