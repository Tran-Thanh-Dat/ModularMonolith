using BuildingBlocks.Application.CQRS;
using BuildingBlocks.Application.Results;
using MediatR;
using Notifications.Application.Abstractions;

namespace Notifications.Application.Notifications.MarkAllNotificationsAsRead;

public sealed record MarkAllNotificationsAsReadCommand : ICommand;

public sealed class MarkAllNotificationsAsReadCommandHandler : IRequestHandler<MarkAllNotificationsAsReadCommand, Result>
{
    private readonly INotificationService _notificationService;
    private readonly INotificationAuthorizationService _notificationAuthorizationService;

    public MarkAllNotificationsAsReadCommandHandler(
        INotificationService notificationService,
        INotificationAuthorizationService notificationAuthorizationService)
    {
        _notificationService = notificationService;
        _notificationAuthorizationService = notificationAuthorizationService;
    }

    public async Task<Result> Handle(MarkAllNotificationsAsReadCommand request, CancellationToken cancellationToken)
    {
        var currentUserId = _notificationAuthorizationService.ResolveListUserId(null);
        await _notificationService.MarkAllAsReadAsync(currentUserId, cancellationToken);
        return Result.Success();
    }
}
