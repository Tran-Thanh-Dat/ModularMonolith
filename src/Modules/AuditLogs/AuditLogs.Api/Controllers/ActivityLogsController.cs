using AuditLogs.Application.ActivityLogs.GetActivityLogById;
using AuditLogs.Application.ActivityLogs.GetActivityLogs;
using AuditLogs.Application.Permissions;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditLogs.Api.Controllers;

[Authorize]
[Route("api/activity-logs")]
public sealed class ActivityLogsController : BaseApiController
{
    private readonly ISender _sender;

    public ActivityLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(ActivityLogsPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<ActivityLogListItemResponse>>> GetActivityLogs(
        [FromQuery] string? keyword,
        [FromQuery] string? activityType,
        [FromQuery] string? moduleName,
        [FromQuery] Guid? userId,
        [FromQuery] string? userName,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetActivityLogsQuery(
                keyword,
                activityType,
                moduleName,
                userId,
                userName,
                status,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(ActivityLogsPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<ActivityLogDetailResponse>>> GetActivityLogById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetActivityLogByIdQuery(id), cancellationToken);
        return FromResult(result);
    }
}
