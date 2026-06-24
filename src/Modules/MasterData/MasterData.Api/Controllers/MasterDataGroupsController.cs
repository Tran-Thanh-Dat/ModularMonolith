using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MasterData.Api.Contracts;
using MasterData.Application.Dtos;
using MasterData.Application.Groups;
using MasterData.Application.Permissions;
using MasterData.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MasterData.Api.Controllers;

[Authorize]
[Route("api/v1/master-data-groups")]
public sealed class MasterDataGroupsController : BaseApiController
{
    private readonly ISender _sender;

    public MasterDataGroupsController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(MasterDataPermissionCodes.GroupView)]
    public async Task<ActionResult<PagedResponse<MasterDataGroupListItemResponse>>> GetGroups(
        [FromQuery] string? keyword,
        [FromQuery] string? code,
        [FromQuery] MasterDataScope? scope,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? organizationId,
        [FromQuery] bool? isSystem,
        [FromQuery] bool? isActive,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetMasterDataGroupsQuery(keyword, code, scope, tenantId, organizationId, isSystem, isActive, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.GroupView)]
    public async Task<ActionResult<ApiResponse<MasterDataGroupDetailResponse>>> GetGroupById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMasterDataGroupByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(MasterDataPermissionCodes.GroupManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateMasterDataGroupResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateMasterDataGroupResponse>>> CreateGroup(
        [FromBody] CreateMasterDataGroupRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<MasterDataScope>(request.Scope, true, out var scope))
        {
            return BadRequest(ApiResponse.Fail("MasterDataGroup.InvalidScope", "Invalid scope value."));
        }

        var result = await _sender.Send(
            new CreateMasterDataGroupCommand(
                request.Code,
                request.Name,
                request.Description,
                scope,
                request.TenantId,
                request.OrganizationId,
                request.SortOrder,
                request.Metadata),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetGroupById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.GroupManage)]
    public async Task<ActionResult<ApiResponse>> UpdateGroup(
        Guid id,
        [FromBody] UpdateMasterDataGroupRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMasterDataGroupCommand(id, request.Code, request.Name, request.Description, request.SortOrder, request.Metadata),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.GroupManage)]
    public async Task<ActionResult<ApiResponse>> DeleteGroup(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteMasterDataGroupCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(MasterDataPermissionCodes.GroupManage)]
    public async Task<ActionResult<ApiResponse>> ActivateGroup(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateMasterDataGroupCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(MasterDataPermissionCodes.GroupManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateGroup(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateMasterDataGroupCommand(id), cancellationToken);
        return FromResult(result);
    }
}
