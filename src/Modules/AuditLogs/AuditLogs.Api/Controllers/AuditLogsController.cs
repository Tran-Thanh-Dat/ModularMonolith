using AuditLogs.Application.ActivityLogs.GetActivityLogById;
using AuditLogs.Application.ActivityLogs.GetActivityLogs;
using AuditLogs.Application.AuditLogs.GetAuditLogById;
using AuditLogs.Application.AuditLogs.GetAuditLogs;
using AuditLogs.Application.Permissions;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditLogs.Api.Controllers;

[Authorize]
[Route("api/audit-logs")]
public sealed class AuditLogsController : BaseApiController
{
    private readonly ISender _sender;

    public AuditLogsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasAnyPermission(AuditLogsPermissionCodes.View, AuditLogsPermissionCodes.LegacyView)]
    public async Task<ActionResult<PagedResponse<AuditLogListItemResponse>>> GetAuditLogs(
        [FromQuery] string? keyword,
        [FromQuery] string? moduleName,
        [FromQuery] string? action,
        [FromQuery] Guid? userId,
        [FromQuery] string? userName,
        [FromQuery] string? entityName,
        [FromQuery] string? entityId,
        [FromQuery] string? status,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAuditLogsQuery(
                keyword,
                moduleName,
                action,
                userId,
                userName,
                entityName,
                entityId,
                status,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasAnyPermission(AuditLogsPermissionCodes.View, AuditLogsPermissionCodes.LegacyView)]
    public async Task<ActionResult<ApiResponse<AuditLogDetailResponse>>> GetAuditLogById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAuditLogByIdQuery(id), cancellationToken);
        return FromResult(result);
    }
}
