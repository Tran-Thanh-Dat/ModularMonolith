using BuildingBlocks.Web.Authorization;
using BuildingBlocks.Web.Controllers;
using BuildingBlocks.Web.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notifications.Api.Contracts;
using Notifications.Application.Notifications;
using Notifications.Application.Notifications.ArchiveNotification;
using Notifications.Application.Notifications.CreateNotification;
using Notifications.Application.Notifications.GetNotificationById;
using Notifications.Application.Notifications.GetNotifications;
using Notifications.Application.Notifications.MarkAllNotificationsAsRead;
using Notifications.Application.Notifications.MarkNotificationAsRead;
using Notifications.Application.Permissions;

namespace Notifications.Api.Controllers;

[Authorize]
[Route("api/v1/notifications")]
public sealed class NotificationsController : BaseApiController
{
    private readonly ISender _sender;

    public NotificationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [HasPermission(NotificationsPermissionCodes.NotificationView)]
    public async Task<ActionResult<PagedResponse<NotificationListItemResponse>>> GetNotifications(
        [FromQuery] Guid? userId,
        [FromQuery] string? keyword,
        [FromQuery] string? type,
        [FromQuery] string? status,
        [FromQuery] bool? isRead,
        [FromQuery] string? referenceType,
        [FromQuery] string? referenceId,
        [FromQuery] DateTimeOffset? fromDate,
        [FromQuery] DateTimeOffset? toDate,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetNotificationsQuery(
                userId,
                keyword,
                type,
                status,
                isRead,
                referenceType,
                referenceId,
                fromDate,
                toDate,
                pageIndex,
                pageSize),
            cancellationToken);

        return FromPagedResult(result);
    }

    [HttpGet("{id:guid}")]
    [HasPermission(NotificationsPermissionCodes.NotificationView)]
    public async Task<ActionResult<ApiResponse<NotificationDetailResponse>>> GetNotificationById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetNotificationByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPost]
    [HasPermission(NotificationsPermissionCodes.NotificationCreate)]
    public async Task<ActionResult<ApiResponse<CreateNotificationResponse>>> CreateNotification(
        [FromBody] CreateNotificationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateNotificationCommand(
                request.UserId,
                request.Title,
                request.Message,
                request.Type,
                request.ReferenceType,
                request.ReferenceId,
                request.Metadata),
            cancellationToken);

        return CreatedFromResult(
            nameof(GetNotificationById),
            new { id = result.IsSuccess ? result.Data!.Id : Guid.Empty },
            result);
    }

    [HttpPatch("{id:guid}/read")]
    [HasPermission(NotificationsPermissionCodes.NotificationMarkRead)]
    public async Task<ActionResult<ApiResponse>> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("read-all")]
    [HasPermission(NotificationsPermissionCodes.NotificationMarkRead)]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);
        return FromResult(result);
    }

    [HttpPatch("{id:guid}/archive")]
    [HasPermission(NotificationsPermissionCodes.NotificationArchive)]
    public async Task<ActionResult<ApiResponse>> Archive(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ArchiveNotificationCommand(id), cancellationToken);
        return FromResult(result);
    }
}
