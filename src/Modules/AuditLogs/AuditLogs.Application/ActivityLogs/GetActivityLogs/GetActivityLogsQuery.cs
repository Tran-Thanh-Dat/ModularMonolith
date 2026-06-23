using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;

namespace AuditLogs.Application.ActivityLogs.GetActivityLogs;

public sealed record GetActivityLogsQuery(
    string? Keyword,
    string? ActivityType,
    string? ModuleName,
    Guid? UserId,
    string? UserName,
    string? Status,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<ActivityLogListItemResponse>>;

public sealed class GetActivityLogsQueryValidator : AbstractValidator<GetActivityLogsQuery>
{
    public GetActivityLogsQueryValidator()
    {
        RuleFor(query => query.PageIndex).GreaterThanOrEqualTo(1);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue || !query.ToDate.HasValue || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must be less than or equal to ToDate.");
    }
}

public sealed class GetActivityLogsQueryHandler : IRequestHandler<GetActivityLogsQuery, Result<PagedResult<ActivityLogListItemResponse>>>
{
    private readonly IActivityLogReadService _activityLogReadService;

    public GetActivityLogsQueryHandler(IActivityLogReadService activityLogReadService)
    {
        _activityLogReadService = activityLogReadService;
    }

    public async Task<Result<PagedResult<ActivityLogListItemResponse>>> Handle(
        GetActivityLogsQuery request,
        CancellationToken cancellationToken)
    {
        var result = await _activityLogReadService.GetActivityLogsAsync(
            request.Keyword,
            request.ActivityType,
            request.ModuleName,
            request.UserId,
            request.UserName,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<ActivityLogListItemResponse>>.Success(result);
    }
}
