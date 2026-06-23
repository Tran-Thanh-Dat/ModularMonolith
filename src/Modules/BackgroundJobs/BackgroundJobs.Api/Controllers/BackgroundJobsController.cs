using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using BackgroundJobs.Api.Contracts;
using BackgroundJobs.Application.JobExecutions;
using BackgroundJobs.Application.JobExecutions.GetBackgroundJobExecutionById;
using BackgroundJobs.Application.JobExecutions.GetBackgroundJobExecutions;
using BackgroundJobs.Application.JobExecutions.RunEmailRetryJob;
using BackgroundJobs.Application.JobExecutions.RunLogCleanupJob;
using BackgroundJobs.Application.JobExecutions.RunTemporaryFileCleanupJob;
using BackgroundJobs.Application.Permissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackgroundJobs.Api.Controllers;

[Authorize]
[Route("api/v1/background-jobs")]
public sealed class BackgroundJobsController : BaseApiController
{
    private readonly ISender _sender;

    public BackgroundJobsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("executions")]
    [HasPermission(BackgroundJobsPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<BackgroundJobExecutionListItemResponse>>> GetExecutions(
        [FromQuery] string? keyword,
        [FromQuery] string? jobName,
        [FromQuery] string? jobType,
        [FromQuery] string? status,
        [FromQuery] string? triggerSource,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetBackgroundJobExecutionsQuery(
                keyword,
                jobName,
                jobType,
                status,
                triggerSource,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("executions/{id:guid}")]
    [HasPermission(BackgroundJobsPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<BackgroundJobExecutionDetailResponse>>> GetExecutionById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBackgroundJobExecutionByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("email-retry/run")]
    [HasPermission(BackgroundJobsPermissionCodes.Run)]
    public async Task<ActionResult<ApiResponse<RunBackgroundJobResponse>>> RunEmailRetry(
        [FromBody] RunEmailRetryJobRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RunEmailRetryJobCommand(request?.BatchSize),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("temporary-files-cleanup/run")]
    [HasPermission(BackgroundJobsPermissionCodes.Run)]
    public async Task<ActionResult<ApiResponse<RunBackgroundJobResponse>>> RunTemporaryFileCleanup(
        [FromBody] RunTemporaryFileCleanupJobRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RunTemporaryFileCleanupJobCommand(request?.BatchSize),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPost("log-cleanup/run")]
    [HasPermission(BackgroundJobsPermissionCodes.Run)]
    public async Task<ActionResult<ApiResponse<RunBackgroundJobResponse>>> RunLogCleanup(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RunLogCleanupJobCommand(), cancellationToken);
        return FromResult(result);
    }
}
