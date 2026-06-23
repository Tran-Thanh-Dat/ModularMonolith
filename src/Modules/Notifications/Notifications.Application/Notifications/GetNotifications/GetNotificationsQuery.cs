using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Pagination;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;
using Notifications.Application.Validation;

namespace Notifications.Application.Notifications.GetNotifications;

public sealed record GetNotificationsQuery(
    Guid? UserId,
    string? Keyword,
    string? Type,
    string? Status,
    bool? IsRead,
    string? ReferenceType,
    string? ReferenceId,
    DateTimeOffset? FromDate,
    DateTimeOffset? ToDate,
    int PageIndex = 1,
    int PageSize = 20) : IQuery<PagedResult<NotificationListItemResponse>>;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(query => query.PageIndex).ValidPageIndex();
        RuleFor(query => query.PageSize).ValidPageSize();
    }
}

public sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<PagedResult<NotificationListItemResponse>>>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationAuthorizationService _notificationAuthorizationService;

    public GetNotificationsQueryHandler(
        INotificationService notificationService,
        INotificationAuthorizationService notificationAuthorizationService)
    {
        _notificationService = notificationService;
        _notificationAuthorizationService = notificationAuthorizationService;
    }

    public async Task<Result<PagedResult<NotificationListItemResponse>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var effectiveUserId = _notificationAuthorizationService.ResolveListUserId(request.UserId);

        var result = await _notificationService.GetListAsync(
            effectiveUserId,
            request.Keyword,
            request.Type,
            request.Status,
            request.IsRead,
            request.ReferenceType,
            request.ReferenceId,
            request.FromDate,
            request.ToDate,
            request.PageIndex,
            request.PageSize,
            cancellationToken);

        return Result<PagedResult<NotificationListItemResponse>>.Success(result);
    }
}
