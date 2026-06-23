using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Users.Api.Contracts;
using Users.Application.Permissions;
using Users.Application.Users.ActivateUser;
using Users.Application.Users.AssignPermissions;
using Users.Application.Users.AssignRoles;
using Users.Application.Users.CreateUser;
using Users.Application.Users.DeactivateUser;
using Users.Application.Users.GetUserById;
using Users.Application.Users.GetUsers;
using Users.Application.Users.RemovePermission;
using Users.Application.Users.RemoveRole;
using Users.Application.Users.UpdateUser;

namespace Users.Api.Controllers;

[Authorize]
[Route("api/v1/users")]
public sealed class UsersController : BaseApiController
{
    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(UsersPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<UserListItemResponse>>> GetUsers(
        [FromQuery] string? keyword,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetUsersQuery(keyword, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(UsersPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<UserDetailResponse>>> GetUserById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(UsersPermissionCodes.Create)]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateUserResponse>>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateUserCommand(
                request.UserName,
                request.Email,
                request.FullName,
                request.Password,
                request.RoleIds ?? []),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetUserById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(UsersPermissionCodes.Update)]
    public async Task<ActionResult<ApiResponse>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateUserCommand(id, request.Email, request.FullName),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("{id:guid}/activate")]
    [HasPermission(UsersPermissionCodes.Activate)]
    public async Task<ActionResult<ApiResponse>> ActivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/deactivate")]
    [HasPermission(UsersPermissionCodes.Deactivate)]
    public async Task<ActionResult<ApiResponse>> DeactivateUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/roles")]
    [HasPermission(UsersPermissionCodes.AssignRole)]
    public async Task<ActionResult<ApiResponse>> AssignRoles(
        Guid id,
        [FromBody] AssignRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AssignRolesToUserCommand(id, request.RoleIds),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [HasPermission(UsersPermissionCodes.AssignRole)]
    public async Task<ActionResult<ApiResponse>> RemoveRole(
        Guid id,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemoveRoleFromUserCommand(id, roleId), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/permissions")]
    [HasPermission(UsersPermissionCodes.AssignPermission)]
    public async Task<ActionResult<ApiResponse>> AssignPermissions(
        Guid id,
        [FromBody] AssignPermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new AssignPermissionsToUserCommand(id, request.PermissionIds),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}/permissions/{permissionId:guid}")]
    [HasPermission(UsersPermissionCodes.AssignPermission)]
    public async Task<ActionResult<ApiResponse>> RemovePermission(
        Guid id,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RemovePermissionFromUserCommand(id, permissionId), cancellationToken);
        return FromResult(result);
    }
}
