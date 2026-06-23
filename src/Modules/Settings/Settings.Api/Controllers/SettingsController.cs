using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Settings.Api.Contracts;
using Settings.Application.Permissions;
using Settings.Application.Settings;
using Settings.Application.Settings.ActivateSetting;
using Settings.Application.Settings.CreateSetting;
using Settings.Application.Settings.DeactivateSetting;
using Settings.Application.Settings.DeleteSetting;
using Settings.Application.Settings.GetSettingById;
using Settings.Application.Settings.GetSettingByKey;
using Settings.Application.Settings.GetSettings;
using Settings.Application.Settings.GetSettingsByGroup;
using Settings.Application.Settings.UpdateSetting;
using Settings.Application.Settings.UpdateSettingValue;

namespace Settings.Api.Controllers;

[Authorize]
[Route("api/v1/settings")]
public sealed class SettingsController : BaseApiController
{
    private readonly ISender _sender;

    public SettingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(SettingsPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<SettingListItemResponse>>> GetSettings(
        [FromQuery] string? keyword,
        [FromQuery] string? group,
        [FromQuery] string? dataType,
        [FromQuery] bool? isActive,
        [FromQuery] bool? isSystem,
        [FromQuery] bool? isEditable,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetSettingsQuery(
                keyword,
                group,
                dataType,
                isActive,
                isSystem,
                isEditable,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(SettingsPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<SettingDetailResponse>>> GetSettingById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSettingByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("key/{key}")]
    [HasPermission(SettingsPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<SettingDetailResponse>>> GetSettingByKey(
        string key,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSettingByKeyQuery(key), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("groups/{group}")]
    [HasPermission(SettingsPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SettingListItemResponse>>>> GetSettingsByGroup(
        string group,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSettingsByGroupQuery(group), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(SettingsPermissionCodes.Create)]
    [ProducesResponseType(typeof(ApiResponse<CreateSettingResponse>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CreateSettingResponse>>> CreateSetting(
        [FromBody] Contracts.CreateSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateSettingCommand(
                request.Key,
                request.Group,
                request.Name,
                request.Description,
                request.Value,
                request.DefaultValue,
                request.DataType,
                request.IsEncrypted,
                request.IsSensitive,
                request.IsSystem,
                request.IsEditable,
                request.SortOrder),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetSettingById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPut("{id:guid}")]
    [HasPermission(SettingsPermissionCodes.Update)]
    public async Task<ActionResult<ApiResponse>> UpdateSetting(
        Guid id,
        [FromBody] Contracts.UpdateSettingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateSettingCommand(
                id,
                request.Name,
                request.Description,
                request.SortOrder,
                request.IsEditable),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPatch("{id:guid}/value")]
    [HasPermission(SettingsPermissionCodes.Update)]
    public async Task<ActionResult<ApiResponse>> UpdateSettingValue(
        Guid id,
        [FromBody] Contracts.UpdateSettingValueRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new UpdateSettingValueCommand(id, request.Value),
            cancellationToken);

        return FromResult(result);
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(SettingsPermissionCodes.Delete)]
    public async Task<ActionResult<ApiResponse>> DeleteSetting(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteSettingCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [HasPermission(SettingsPermissionCodes.Activate)]
    public async Task<ActionResult<ApiResponse>> ActivateSetting(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateSettingCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/deactivate")]
    [HasPermission(SettingsPermissionCodes.Deactivate)]
    public async Task<ActionResult<ApiResponse>> DeactivateSetting(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateSettingCommand(id), cancellationToken);
        return FromResult(result);
    }
}
