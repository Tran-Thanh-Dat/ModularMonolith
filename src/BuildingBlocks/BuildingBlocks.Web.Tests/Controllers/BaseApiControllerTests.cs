using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Web.Constants;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BuildingBlocks.Web.Tests.Controllers;

public sealed class BaseApiControllerTests
{
    private readonly TestApiController _controller = new();

    public BaseApiControllerTests()
    {
        SetHttpContext(traceIdentifier: "default-trace-id");
    }

    [Fact]
    public void FromResult_WhenSuccess_Returns200WithApiResponseDataAndTraceId()
    {
        SetCorrelationId("success-correlation-id");

        var actionResult = _controller.FromResult(Result<string>.Success("payload"));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        var response = Assert.IsType<ApiResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("payload", response.Data);
        Assert.Equal("success-correlation-id", response.TraceId);
    }

    [Theory]
    [InlineData(CommonErrors.ValidationError, 400)]
    [InlineData(CommonErrors.Unauthorized, 401)]
    [InlineData(AuthErrors.InvalidCredentials, 401)]
    [InlineData(CommonErrors.Forbidden, 403)]
    [InlineData(CommonErrors.NotFound, 404)]
    [InlineData(CommonErrors.Conflict, 409)]
    [InlineData(CategoryErrors.CodeAlreadyExists, 409)]
    [InlineData(CommonErrors.UnknownError, 500)]
    public void FromResult_WhenFailure_MapsToExpectedStatusCode(string code, int expectedStatus)
    {
        SetCorrelationId("failure-correlation-id");

        var actionResult = _controller.FromResult(
            Result<string>.Failure(code, "Operation failed."));

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(expectedStatus, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse<string>>(objectResult.Value);
        Assert.False(response.Success);
        Assert.Equal(code, response.Code);
        Assert.Equal("failure-correlation-id", response.TraceId);
    }

    [Fact]
    public void FromResultNonGeneric_WhenFailure_MapsValidationErrorTo400()
    {
        var actionResult = _controller.FromResult(
            Result.Failure(CommonErrors.ValidationError, "Validation failed."));

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, objectResult.StatusCode);
        var response = Assert.IsType<ApiResponse>(objectResult.Value);
        Assert.Equal(CommonErrors.ValidationError, response.Code);
    }

    [Fact]
    public void CreatedFromResult_WhenSuccess_Returns201WithLocation()
    {
        var id = Guid.NewGuid();
        var actionResult = _controller.CreatedFromResult(
            nameof(TestApiController.GetItem),
            new { id },
            Result<ItemResponse>.Success(new ItemResponse(id, "Created")));

        var createdResult = Assert.IsType<CreatedAtActionResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal(nameof(TestApiController.GetItem), createdResult.ActionName);
        var response = Assert.IsType<ApiResponse<ItemResponse>>(createdResult.Value);
        Assert.True(response.Success);
        Assert.Equal(id, response.Data!.Id);
    }

    [Fact]
    public void FromPagedResult_WhenSuccess_ReturnsPagedResponseWithMetadata()
    {
        SetCorrelationId("paged-correlation-id");

        var paged = new PagedResult<string>
        {
            Items = ["a", "b"],
            PageNumber = 2,
            PageSize = 10,
            TotalCount = 25
        };

        var actionResult = _controller.FromPagedResult(Result<PagedResult<string>>.Success(paged));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<PagedResponse<string>>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(2, response.Pagination!.PageIndex);
        Assert.Equal(10, response.Pagination.PageSize);
        Assert.Equal(25, response.Pagination.TotalItems);
        Assert.Equal(3, response.Pagination.TotalPages);
        Assert.True(response.Pagination.HasPreviousPage);
        Assert.True(response.Pagination.HasNextPage);
        Assert.Equal("paged-correlation-id", response.TraceId);
    }

    [Fact]
    public void OkResponse_UsesRequestHeaderCorrelationIdWhenItemsMissing()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[HeaderNames.XCorrelationId] = "header-correlation-id";
        context.TraceIdentifier = "trace-id-fallback";
        _controller.ControllerContext = new ControllerContext { HttpContext = context };

        var actionResult = _controller.PublicOkResponse("ok");

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var response = Assert.IsType<ApiResponse>(okResult.Value);
        Assert.Equal("header-correlation-id", response.TraceId);
    }

    private void SetCorrelationId(string correlationId) =>
        SetHttpContext(correlationId: correlationId);

    private void SetHttpContext(string? correlationId = null, string? traceIdentifier = null)
    {
        var context = new DefaultHttpContext();
        if (correlationId is not null)
        {
            context.Items["CorrelationId"] = correlationId;
        }

        if (traceIdentifier is not null)
        {
            context.TraceIdentifier = traceIdentifier;
        }

        _controller.ControllerContext = new ControllerContext { HttpContext = context };
    }

    private sealed class TestApiController : BaseApiController
    {
        public ActionResult<ApiResponse<string>> FromResult(Result<string> result) => base.FromResult(result);

        public new ActionResult<ApiResponse> FromResult(Result result) => base.FromResult(result);

        public ActionResult<ApiResponse<ItemResponse>> CreatedFromResult(
            string actionName,
            object routeValues,
            Result<ItemResponse> result) =>
            base.CreatedFromResult(actionName, routeValues, result);

        public ActionResult<PagedResponse<string>> FromPagedResult(Result<PagedResult<string>> result) =>
            base.FromPagedResult(result);

        public ActionResult<ApiResponse> PublicOkResponse(string? message = null) => OkResponse(message);

        [HttpGet("{id:guid}")]
        public IActionResult GetItem(Guid id) => Ok();
    }

    private sealed record ItemResponse(Guid Id, string Name);
}
