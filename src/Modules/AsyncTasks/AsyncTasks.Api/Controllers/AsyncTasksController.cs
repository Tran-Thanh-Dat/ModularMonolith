using AsyncTasks.Application.Commands;
using AsyncTasks.Api.Contracts;
using AsyncTasks.Application.Dtos;
using AsyncTasks.Application.Permissions;
using AsyncTasks.Application.Queries;
using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AsyncTasks.Api.Controllers;

[Authorize]
[Route("api/v1/async-tasks")]
public sealed class AsyncTasksController : BaseApiController
{
    private readonly ISender _sender;

    public AsyncTasksController(ISender sender) => _sender = sender;

    [HttpPost("email-demo")]
    [HasPermission(AsyncTaskPermissionCodes.Submit)]
    [ProducesResponseType(typeof(ApiResponse<AsyncTaskDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> SubmitEmailDemo(
        [FromBody] SubmitEmailDemoTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitEmailDemoTaskCommand(request.EmailTo, request.Subject, request.Body, request.TenantId, request.OrganizationId),
            cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("file-processing-demo")]
    [HasPermission(AsyncTaskPermissionCodes.Submit)]
    [ProducesResponseType(typeof(ApiResponse<AsyncTaskDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> SubmitFileProcessingDemo(
        [FromBody] SubmitFileProcessingDemoTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitFileProcessingDemoTaskCommand(request.FileId, request.FileName, request.TenantId, request.OrganizationId),
            cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("fail-demo")]
    [HasPermission(AsyncTaskPermissionCodes.Submit)]
    [ProducesResponseType(typeof(ApiResponse<AsyncTaskDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> SubmitFailDemo(
        [FromBody] SubmitFailDemoTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitFailDemoTaskCommand(request.FailReason, request.ShouldAlwaysFail, request.TenantId, request.OrganizationId),
            cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPost("long-running-demo")]
    [HasPermission(AsyncTaskPermissionCodes.Submit)]
    [ProducesResponseType(typeof(ApiResponse<AsyncTaskDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> SubmitLongRunningDemo(
        [FromBody] SubmitLongRunningDemoTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SubmitLongRunningDemoTaskCommand(request.DurationSeconds, request.Steps, request.TenantId, request.OrganizationId),
            cancellationToken);
        return CreatedFromResult(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpGet]
    [HasPermission(AsyncTaskPermissionCodes.View)]
    public async Task<ActionResult<PagedResponse<AsyncTaskDto>>> GetTasks(
        [FromQuery] string? keyword,
        [FromQuery] string? taskNo,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] Guid? requestedByUserId,
        [FromQuery] Guid? tenantId,
        [FromQuery] Guid? organizationId,
        [FromQuery] DateTimeOffset? createdFrom,
        [FromQuery] DateTimeOffset? createdTo,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetAsyncTasksQuery(keyword, taskNo, type, status, requestedByUserId, tenantId, organizationId, createdFrom, createdTo, pageIndex, pageSize),
            cancellationToken);
        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(AsyncTaskPermissionCodes.View)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetAsyncTaskByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/cancel")]
    [HasPermission(AsyncTaskPermissionCodes.Cancel)]
    public async Task<ActionResult<ApiResponse>> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CancelAsyncTaskCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/retry")]
    [HasPermission(AsyncTaskPermissionCodes.Retry)]
    public async Task<ActionResult<ApiResponse<AsyncTaskDto>>> Retry(Guid id, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RetryAsyncTaskCommand(id), cancellationToken);
        return FromResult(result);
    }
}
