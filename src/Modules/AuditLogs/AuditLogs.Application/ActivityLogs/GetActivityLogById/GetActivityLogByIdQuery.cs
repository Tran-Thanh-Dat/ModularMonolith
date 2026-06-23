using AuditLogs.Application.Abstractions;
using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using MediatR;

namespace AuditLogs.Application.ActivityLogs.GetActivityLogById;

public sealed record GetActivityLogByIdQuery(Guid Id) : IQuery<ActivityLogDetailResponse>;

public sealed class GetActivityLogByIdQueryHandler : IRequestHandler<GetActivityLogByIdQuery, Result<ActivityLogDetailResponse>>
{
    private readonly IActivityLogReadService _activityLogReadService;

    public GetActivityLogByIdQueryHandler(IActivityLogReadService activityLogReadService)
    {
        _activityLogReadService = activityLogReadService;
    }

    public async Task<Result<ActivityLogDetailResponse>> Handle(
        GetActivityLogByIdQuery request,
        CancellationToken cancellationToken)
    {
        var activityLog = await _activityLogReadService.GetActivityLogByIdAsync(request.Id, cancellationToken);

        if (activityLog is null)
        {
            throw new NotFoundException(
                CommonErrors.NotFound,
                $"Activity log '{request.Id}' was not found.");
        }

        return Result<ActivityLogDetailResponse>.Success(activityLog);
    }
}
