using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Notifications.Application.Abstractions;
using Notifications.Application.EmailTemplates;
using Notifications.Application.Validation;
using Notifications.Domain.EmailTemplates;
using Notifications.Infrastructure.Caching;
using Notifications.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Notifications.Infrastructure.Services;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly NotificationsUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<EmailTemplateService> _logger;

    public EmailTemplateService(
        NotificationsUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<EmailTemplateService> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _activityLogService = activityLogService;
        _cacheService = cacheService;
        _cacheInvalidationBuffer = cacheInvalidationBuffer;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(
        string code,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim();

        if (await CodeExistsAsync(normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                EmailErrors.TemplateCodeAlreadyExists,
                $"Email template code '{normalizedCode}' already exists.");
        }

        var template = EmailTemplate.Create(
            normalizedCode,
            name,
            subject,
            body,
            isHtml,
            description,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await _unitOfWork.Repository<EmailTemplate, Guid>().AddAsync(template, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created email template: {template.Code}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateEmailTemplateListCache();

        return template.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string subject,
        string body,
        bool isHtml,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateForUpdateAsync(id, cancellationToken);
        template.Update(name, subject, body, isHtml, description);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated email template: {template.Code}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateEmailTemplateCache(template.Code);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateForUpdateAsync(id, cancellationToken);
        template.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted email template: {template.Code}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateEmailTemplateCache(template.Code);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateForUpdateAsync(id, cancellationToken);

        if (template.IsActive)
        {
            return;
        }

        template.Activate();

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated email template: {template.Code}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateEmailTemplateCache(template.Code);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var template = await GetTemplateForUpdateAsync(id, cancellationToken);

        if (!template.IsActive)
        {
            return;
        }

        template.Deactivate();

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated email template: {template.Code}",
            AuditLogConstants.Modules.Notifications,
            cancellationToken: cancellationToken);

        InvalidateEmailTemplateCache(template.Code);
    }

    public async Task<EmailTemplateDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var template = await _unitOfWork.Repository<EmailTemplate, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        return template is null ? null : MapDetail(template);
    }

    public async Task<PagedResult<EmailTemplateListItemResponse>> GetListAsync(
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
        var query = _unitOfWork.Repository<EmailTemplate, Guid>().QueryReadOnly().Where(t => !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var pattern = $"%{keyword.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.Code, pattern) ||
                EF.Functions.ILike(t.Name, pattern) ||
                EF.Functions.ILike(t.Subject, pattern) ||
                EF.Functions.ILike(t.Description ?? string.Empty, pattern));
        }

        if (isActive.HasValue)
        {
            query = query.Where(t => t.IsActive == isActive.Value);
        }

        var projected = query
            .OrderBy(t => t.Code)
            .Select(t => new EmailTemplateListItemResponse
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Subject = t.Subject,
                IsHtml = t.IsHtml,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            });

        return await projected.ToPagedResultAsync(pagedRequest, cancellationToken);
    }

    public async Task<(string Subject, string Body, bool IsHtml)> RenderTemplateAsync(
        string templateCode,
        IReadOnlyDictionary<string, string> templateData,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = templateCode.Trim();
        var cacheKey = CacheKeys.EmailTemplateByCode(normalizedCode);

        var cached = await _cacheService.GetOrSetAsync<CachedEmailTemplate?>(
            cacheKey,
            async ct =>
            {
                var template = await _unitOfWork.Repository<EmailTemplate, Guid>()
                    .QueryReadOnly()
                    .FirstOrDefaultAsync(
                        t => t.Code == normalizedCode && !t.IsDeleted,
                        ct);

                if (template is null)
                {
                    return null;
                }

                return new CachedEmailTemplate
                {
                    Subject = template.Subject,
                    Body = template.Body,
                    IsHtml = template.IsHtml,
                    IsActive = template.IsActive
                };
            },
            cancellationToken: cancellationToken);

        if (cached is null)
        {
            throw new NotFoundException(
                EmailErrors.TemplateNotFound,
                $"Email template '{templateCode}' was not found.");
        }

        if (!cached.IsActive)
        {
            throw new BadRequestException(
                EmailErrors.TemplateInactive,
                $"Email template '{templateCode}' is inactive.");
        }

        return (
            TemplateRenderer.Render(cached.Subject, templateData, cached.IsHtml),
            TemplateRenderer.Render(cached.Body, templateData, cached.IsHtml),
            cached.IsHtml);
    }

    private async Task<EmailTemplate> GetTemplateForUpdateAsync(Guid id, CancellationToken cancellationToken)
    {
        var template = await _unitOfWork.Repository<EmailTemplate, Guid>()
            .Query()
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);

        if (template is null)
        {
            throw new NotFoundException(
                EmailErrors.TemplateNotFound,
                $"Email template with id '{id}' was not found.");
        }

        return template;
    }

    private async Task<bool> CodeExistsAsync(string code, Guid? excludeId, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<EmailTemplate, Guid>()
            .QueryReadOnly()
            .Where(t => !t.IsDeleted && t.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static EmailTemplateDetailResponse MapDetail(EmailTemplate template) =>
        new()
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Subject = template.Subject,
            Body = template.Body,
            IsHtml = template.IsHtml,
            IsActive = template.IsActive,
            Description = template.Description,
            CreatedAt = template.CreatedAt,
            CreatedBy = template.CreatedBy,
            UpdatedAt = template.UpdatedAt,
            UpdatedBy = template.UpdatedBy
        };

    private void InvalidateEmailTemplateCache(string code)
    {
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.EmailTemplateByCode(code));
        InvalidateEmailTemplateListCache();
    }

    private void InvalidateEmailTemplateListCache() =>
        _cacheInvalidationBuffer.EnqueueRemoveByPrefix(CacheKeys.EmailTemplateListPrefix);
}