using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Results;
using FluentValidation;
using MediatR;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;

namespace Notifications.Application.Notifications.GetNotificationById;

public sealed record GetNotificationByIdQuery(Guid Id) : IQuery<NotificationDetailResponse>;

public sealed class GetNotificationByIdQueryValidator : AbstractValidator<GetNotificationByIdQuery>
{
    public GetNotificationByIdQueryValidator()
    {
        RuleFor(query => query.Id).NotEmpty();
    }
}

public sealed class GetNotificationByIdQueryHandler
    : IRequestHandler<GetNotificationByIdQuery, Result<NotificationDetailResponse>>
{
    private readonly INotificationService _notificationService;

    public GetNotificationByIdQueryHandler(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    public async Task<Result<NotificationDetailResponse>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var notification = await _notificationService.GetByIdAsync(request.Id, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException(
                NotificationErrors.NotFound,
                $"Notification with id '{request.Id}' was not found.");
        }

        return Result<NotificationDetailResponse>.Success(notification);
    }
}
