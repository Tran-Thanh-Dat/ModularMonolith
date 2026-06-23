namespace BuildingBlocks.Web.Responses;

public sealed class ApiError
{
    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string? Field { get; init; }
}

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }

    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public T? Data { get; init; }

    public IReadOnlyList<ApiError>? Errors { get; init; }

    public DateTime Timestamp { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse<T> Ok(
        T data,
        string code = "Common.Success",
        string? message = null,
        string? traceId = null) =>
        new()
        {
            Success = true,
            Code = code,
            Message = message ?? "Success",
            Data = data,
            Timestamp = DateTime.UtcNow,
            TraceId = traceId
        };

    public static ApiResponse<T> Fail(
        string code,
        string message,
        IReadOnlyList<ApiError>? errors = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            TraceId = traceId
        };
}

public sealed class ApiResponse
{
    public bool Success { get; init; }

    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public IReadOnlyList<ApiError>? Errors { get; init; }

    public DateTime Timestamp { get; init; }

    public string? TraceId { get; init; }

    public static ApiResponse Ok(string? message = null, string? traceId = null) =>
        new()
        {
            Success = true,
            Code = "Common.Success",
            Message = message ?? "Success",
            Timestamp = DateTime.UtcNow,
            TraceId = traceId
        };

    public static ApiResponse Fail(
        string code,
        string message,
        IReadOnlyList<ApiError>? errors = null,
        string? traceId = null) =>
        new()
        {
            Success = false,
            Code = code,
            Message = message,
            Errors = errors,
            Timestamp = DateTime.UtcNow,
            TraceId = traceId
        };
}

public sealed class PaginationMeta
{
    public int PageIndex { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPreviousPage { get; init; }

    public bool HasNextPage { get; init; }
}

public sealed class PagedResponse<T>
{
    public bool Success { get; init; }

    public string Code { get; init; } = default!;

    public string Message { get; init; } = default!;

    public IReadOnlyList<T> Data { get; init; } = [];

    public PaginationMeta Pagination { get; init; } = default!;

    public DateTime Timestamp { get; init; }

    public string? TraceId { get; init; }

    public static PagedResponse<T> Ok(
        IReadOnlyList<T> data,
        PaginationMeta pagination,
        string? message = null,
        string? traceId = null) =>
        new()
        {
            Success = true,
            Code = "Common.Success",
            Message = message ?? "Success",
            Data = data,
            Pagination = pagination,
            Timestamp = DateTime.UtcNow,
            TraceId = traceId
        };
}
