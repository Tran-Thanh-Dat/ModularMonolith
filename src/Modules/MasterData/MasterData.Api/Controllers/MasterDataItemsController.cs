using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MasterData.Api.Contracts;
using MasterData.Application.Dtos;
using MasterData.Application.Items;
using MasterData.Application.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MasterData.Api.Controllers;

[Authorize]
[Route("api/v1/master-data-items")]
public sealed class MasterDataItemsController : BaseApiController
{
    private readonly ISender _sender;

    public MasterDataItemsController(ISender sender) => _sender = sender;

    [HttpGet]
    [HasPermission(MasterDataPermissionCodes.ItemView)]
    public async Task<ActionResult<PagedResponse<MasterDataItemListItemResponse>>> GetItems(
        [FromQuery] Guid? groupId,
        [FromQuery] string? groupCode,
        [FromQuery] string? keyword,
        [FromQuery] string? code,
        [FromQuery] Guid? parentItemId,
        [FromQuery] bool? isSystem,
        [FromQuery] bool? isDefault,
        [FromQuery] bool? isActive,
        [FromQuery] DateTimeOffset? effectiveAt,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetMasterDataItemsQuery(groupId, groupCode, keyword, code, parentItemId, isSystem, isDefault, isActive, effectiveAt, pageIndex, pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.ItemView)]
    public async Task<ActionResult<ApiResponse<MasterDataItemDetailResponse>>> GetItemById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetMasterDataItemByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    [ProducesResponseType(typeof(ApiResponse<CreateMasterDataItemResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateMasterDataItemResponse>>> CreateItem(
        [FromBody] CreateMasterDataItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateMasterDataItemCommand(
                request.GroupId,
                request.Code,
                request.Name,
                request.Value,
                request.Description,
                request.ParentItemId,
                request.IsDefault,
                request.SortOrder,
                request.EffectiveFrom,
                request.EffectiveTo,
                request.Metadata),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetItemById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    public async Task<ActionResult<ApiResponse>> UpdateItem(
        Guid id,
        [FromBody] UpdateMasterDataItemRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateMasterDataItemCommand(
                id,
                request.Code,
                request.Name,
                request.Value,
                request.Description,
                request.ParentItemId,
                request.SortOrder,
                request.EffectiveFrom,
                request.EffectiveTo,
                request.Metadata),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    public async Task<ActionResult<ApiResponse>> DeleteItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteMasterDataItemCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    public async Task<ActionResult<ApiResponse>> ActivateItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateMasterDataItemCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    public async Task<ActionResult<ApiResponse>> DeactivateItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateMasterDataItemCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/set-default")]
    [HasPermission(MasterDataPermissionCodes.ItemManage)]
    public async Task<ActionResult<ApiResponse>> SetDefaultItem(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SetDefaultMasterDataItemCommand(id), cancellationToken);
        return FromResult(result);
    }
}
