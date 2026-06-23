using ApiHost.Diagnostics;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ApiHost.Controllers;

[ApiController]
[Route("api/v1/diagnostics")]
public sealed class DiagnosticsController : BaseApiController
{
    private readonly IWebHostEnvironment _environment;
    private readonly IMediator _mediator;

    public DiagnosticsController(IWebHostEnvironment environment, IMediator mediator)
    {
        _environment = environment;
        _mediator = mediator;
    }

    [HttpPost("pipeline")]
    public async Task<ActionResult<ApiResponse<string>>> TestPipeline(
        [FromBody] TestPipelineRequest request,
        CancellationToken cancellationToken)
    {
        EnsureDevelopment();

        var result = await _mediator.Send(new TestPipelineCommand(request.Name), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("pipeline-query")]
    public async Task<ActionResult<ApiResponse<string>>> TestPipelineQuery(
        [FromQuery] string name,
        CancellationToken cancellationToken)
    {
        EnsureDevelopment();

        var result = await _mediator.Send(new TestPipelineQuery(name), cancellationToken);
        return FromResult(result);
    }

    [HttpGet("throw")]
    public IActionResult Throw()
    {
        EnsureDevelopment();
        throw new Exception("Test exception");
    }

    [HttpGet("not-found")]
    public IActionResult NotFoundTest()
    {
        EnsureDevelopment();
        throw new BuildingBlocks.Application.Exceptions.NotFoundException(
            BuildingBlocks.Application.Errors.CommonErrors.NotFound,
            "Resource was not found.");
    }

    [HttpGet("bad-request")]
    public IActionResult BadRequestTest()
    {
        EnsureDevelopment();
        throw new BuildingBlocks.Application.Exceptions.BadRequestException("Invalid request payload.");
    }

    private void EnsureDevelopment()
    {
        if (!_environment.IsDevelopment()
            && !_environment.IsEnvironment("IntegrationTesting"))
        {
            throw new BuildingBlocks.Application.Exceptions.NotFoundException("Endpoint", "diagnostics");
        }
    }
}
