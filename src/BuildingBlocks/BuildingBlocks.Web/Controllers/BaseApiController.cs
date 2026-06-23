using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using BuildingBlocks.Web.Constants;
using BuildingBlocks.Web.Responses;
using Microsoft.AspNetCore.Mvc;

namespace BuildingBlocks.Web.Controllers;

[ApiController]
public abstract class BaseApiController : ControllerBase
{
    protected string TraceId
    {
        get
        {
            if (HttpContext.Items.TryGetValue("CorrelationId", out var value) &&
                value is string correlationId &&
                !string.IsNullOrWhiteSpace(correlationId))
            {
                return correlationId;
            }

            if (HttpContext.Request.Headers.TryGetValue(HeaderNames.XCorrelationId, out var headerValue))
            {
                var headerCorrelationId = headerValue.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(headerCorrelationId))
                {
                    return headerCorrelationId;
                }
            }

            return HttpContext.TraceIdentifier;
        }
    }

    protected ActionResult<ApiResponse<T>> OkResponse<T>(T data, string? message = null) =>
        Ok(ApiResponse<T>.Ok(data, CommonErrors.Success, message, TraceId));

    protected ActionResult<ApiResponse> OkResponse(string? message = null) =>
        Ok(ApiResponse.Ok(message, TraceId));

    protected ActionResult<ApiResponse<T>> CreatedResponse<T>(
        string actionName,
        object routeValues,
        T data,
        string? message = null) =>
        CreatedAtAction(actionName, routeValues, ApiResponse<T>.Ok(data, CommonErrors.Success, message, TraceId));

    protected ActionResult<PagedResponse<T>> OkPagedResponse<T>(
        PagedResult<T> pagedResult,
        string? message = null)
    {
        var pagination = BuildPaginationMeta(pagedResult);
        return Ok(PagedResponse<T>.Ok(pagedResult.Items.ToList(), pagination, message, TraceId));
    }

    protected ActionResult<ApiResponse<T>> FromResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return OkResponse(result.Data!);
        }

        return MapFailure<T>(result);
    }

    protected ActionResult<ApiResponse<T>> CreatedFromResult<T>(
        string actionName,
        object routeValues,
        Result<T> result,
        string? message = null)
    {
        if (result.IsSuccess)
        {
            return CreatedResponse(actionName, routeValues, result.Data!, message);
        }

        return MapFailure<T>(result);
    }

    protected ActionResult<ApiResponse> FromResult(Result result)
    {
        if (result.IsSuccess)
        {
            return OkResponse();
        }

        var response = ApiResponse.Fail(
            result.Code,
            result.Message,
            MapErrors(result.Errors),
            TraceId);

        return StatusCode(ResultStatusMapper.MapStatusCode(result.Code), response);
    }

    protected ActionResult<PagedResponse<T>> FromPagedResult<T>(Result<PagedResult<T>> result)
    {
        if (result.IsSuccess)
        {
            return OkPagedResponse(result.Data!);
        }

        var failureResponse = ApiResponse<T>.Fail(
            result.Code,
            result.Message,
            MapErrors(result.Errors),
            TraceId);

        return StatusCode(ResultStatusMapper.MapStatusCode(result.Code), failureResponse);
    }

    private ActionResult<ApiResponse<T>> MapFailure<T>(Result result)
    {
        var response = ApiResponse<T>.Fail(
            result.Code,
            result.Message,
            MapErrors(result.Errors),
            TraceId);

        return StatusCode(ResultStatusMapper.MapStatusCode(result.Code), response);
    }

    private static PaginationMeta BuildPaginationMeta<T>(PagedResult<T> pagedResult) =>
        new()
        {
            PageIndex = pagedResult.PageNumber,
            PageSize = pagedResult.PageSize,
            TotalItems = pagedResult.TotalCount,
            TotalPages = pagedResult.TotalPages,
            HasPreviousPage = pagedResult.HasPreviousPage,
            HasNextPage = pagedResult.HasNextPage
        };

    private static IReadOnlyList<ApiError> MapErrors(IReadOnlyList<Error> errors) =>
        errors.Select(error => new ApiError
        {
            Code = error.Code,
            Message = error.Message,
            Field = error.Field
        }).ToList();
}
