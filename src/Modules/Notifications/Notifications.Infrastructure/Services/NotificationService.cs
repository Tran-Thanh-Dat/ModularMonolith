using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Notifications.Application.Abstractions;
using Notifications.Application.Notifications;
using Notifications.Domain.Constants;
using Notifications.Domain.Notifications;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Services;

public sealed class NotificationService : INotificationService
{
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationAuthorizationService _notificationAuthorizationService;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        NotificationsUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        INotificationAuthorizationService notificationAuthorizationService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<NotificationService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _notificationAuthorizationService = notificationAuthorizationService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheService = cacheService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        Guid userId,
        string title,
        string message,
        string type,
        string? referenceType,
        string? referenceId,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        var notification = Notification.Create(
            userId,
            title,
            message,
            type,
            referenceType,
            referenceId,
            metadata,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<Notification, Guid>().AddAsync(notification, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created notification: {notification.Title}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateUnreadCountCache(userId);

        return notification.Id;
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await GetNotificationForUpdateAsync(id, cancellationToken);

        if (notification.IsRead && notification.Status == NotificationStatuses.Read)
        {
            return;
        }

        notification.MarkAsRead(_dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Marked notification as read: {notification.Title}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateUnreadCountCache(notification.UserId);
    }

    public async Task MarkAllAsReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _notificationAuthorizationService.EnsureCanManage(userId);

        var notifications = await _unitOfWork.Repository<Notification, Guid>()
            .Query()
            .Where(n => n.UserId == userId && !n.IsDeleted && !n.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count == 0)
        {
            return;
        }

        var now = _dateTimeProvider.UtcNow;
        foreach (var notification in notifications)
        {
            notification.MarkAsRead(now);
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Marked all notifications as read for user {userId}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateUnreadCountCache(userId);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var notification = await GetNotificationForUpdateAsync(id, cancellationToken);

        if (notification.Status == NotificationStatuses.Archived)
        {
            return;
        }

        var wasUnread = !notification.IsRead;
        notification.Archive();

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Archived notification: {notification.Title}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        if (wasUnread)
        {
            InvalidateUnreadCountCache(notification.UserId);
        }
    }

    public async Task<NotificationDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var notification = await _unitOfWork.Repository<Notification, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (notification is null)
        {
            return null;
        }

        _notificationAuthorizationService.EnsureCanView(notification.UserId);
        return MapDetail(notification);
    }

    public async Task<PagedResult<NotificationListItemResponse>> GetListAsync(
        Guid userId,
        string? keyword,
        string? type,
        string? status,
        bool? isRead,
        string? referenceType,
        string? referenceId,
        DateTimeOffset? fromDate,
        DateTimeOffset? toDate,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<Notification, Guid>()
            .QueryReadOnly()
            .Where(n => !n.IsDeleted && n.UserId == userId);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(n =>
                EF.Functions.ILike(n.Title, pattern) ||
                EF.Functions.ILike(n.Message, pattern));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(n => n.Type == type.Trim());
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(n => n.Status == status.Trim());
        }

        if (isRead.HasValue)
        {
            query = query.Where(n => n.IsRead == isRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(referenceType))
        {
            query = query.Where(n => n.ReferenceType == referenceType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            query = query.Where(n => n.ReferenceId == referenceId.Trim());
        }

        if (fromDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(n => n.CreatedAt <= toDate.Value);
        }

        var projected = query
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NotificationListItemResponse
            {
                Id = n.Id,
                UserId = n.UserId,
                Title = n.Title,
                Message = n.Message,
                Type = n.Type,
                Status = n.Status,
                IsRead = n.IsRead,
                ReadAt = n.ReadAt,
                ReferenceType = n.ReferenceType,
                ReferenceId = n.ReferenceId,
                CreatedAt = n.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        _notificationAuthorizationService.EnsureCanView(userId);

        var cacheKey = CacheKeys.NotificationUnreadCount(userId);
        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct => await _unitOfWork.Repository<Notification, Guid>()
                .QueryReadOnly()
                .CountAsync(
                    n => n.UserId == userId && !n.IsDeleted && !n.IsRead,
                    ct),
            TimeSpan.FromMinutes(15),
            cancellationToken);
    }

    private async Task<Notification> GetNotificationForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var notification = await _unitOfWork.Repository<Notification, Guid>()
            .Query()
            .FirstOrDefaultAsync(n => n.Id == id && !n.IsDeleted, cancellationToken);

        if (notification is null)
        {
            throw new NotFoundException(
                NotificationErrors.NotFound,
                $"Notification with id '{id}' was not found.");
        }

        _notificationAuthorizationService.EnsureCanManage(notification.UserId);
        return notification;
    }

    private static NotificationDetailResponse MapDetail(Notification notification) =>
        new()
        {
            Id = notification.Id,
            UserId = notification.UserId,
            Title = notification.Title,
            Message = notification.Message,
            Type = notification.Type,
            Status = notification.Status,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            ReferenceType = notification.ReferenceType,
            ReferenceId = notification.ReferenceId,
            Metadata = notification.Metadata,
            CreatedAt = notification.CreatedAt,
            CreatedBy = notification.CreatedBy,
            UpdatedAt = notification.UpdatedAt,
            UpdatedBy = notification.UpdatedBy
        };

    private void InvalidateUnreadCountCache(Guid userId) =>
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.NotificationUnreadCount(userId));
}