using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Monitoring.Application.Monitoring;
using Monitoring.Application.Monitoring.GetHealthDetails;
using Monitoring.Application.Monitoring.GetSystemInfo;
using Monitoring.Application.Permissions;

namespace Monitoring.Api.Controllers;

[Authorize]
[Route("api/v1/monitoring")]
public sealed class MonitoringController : BaseApiController
{
    private readonly ISender _sender;

    public MonitoringController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("health/details")]
    [HasPermission(MonitoringPermissionCodes.HealthView)]
    public async Task<ActionResult<ApiResponse<HealthDetailsResponse>>> GetHealthDetails(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetHealthDetailsQuery(), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("system-info")]
    [HasPermission(MonitoringPermissionCodes.SystemInfoView)]
    public async Task<ActionResult<ApiResponse<SystemInfoResponse>>> GetSystemInfo(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetSystemInfoQuery(), cancellationToken);
        return FromResult(result);
    }
}
