using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Application.Pagination;
using Categories.Application.Abstractions;
using Categories.Application.Categories;
using Categories.Domain.Categories;
using Categories.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Categories.Infrastructure.Services;

public sealed class CategoryService : ICategoryService
{
    private readonly CategoriesUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IActivityLogService _activityLogService;
    private readonly ICacheService _cacheService;
    private readonly ICacheInvalidationBuffer _cacheInvalidationBuffer;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(
        CategoriesUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IActivityLogService activityLogService,
        ICacheService cacheService,
        ICacheInvalidationBuffer cacheInvalidationBuffer,
        ILogger<CategoryService> logger)
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
        string? description,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim();
        var categories = _unitOfWork.Repository<Category, Guid>();

        if (await CodeExistsAsync(normalizedCode, null, cancellationToken))
        {
            throw new ConflictException(
                CategoryErrors.CodeAlreadyExists,
                $"Category code '{normalizedCode}' already exists.");
        }

        var category = Category.Create(
            normalizedCode,
            name,
            description,
            sortOrder,
            _dateTimeProvider.UtcNow,
            _currentUserService.UserId);

        await categories.AddAsync(category, cancellationToken);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Create,
            $"Created category: {category.Code} - {category.Name}",
            AuditLogConstants.Modules.Categories,
            cancellationToken: cancellationToken);

        InvalidateCategoryListCache();

        _logger.LogInformation("Created category {CategoryId} {Code}", category.Id, category.Code);

        return category.Id;
    }

    public async Task UpdateAsync(
        Guid id,
        string name,
        string? description,
        int sortOrder,
        CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryForUpdateAsync(id, cancellationToken);
        category.Update(name, description, sortOrder);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Update,
            $"Updated category: {category.Code} - {category.Name}",
            AuditLogConstants.Modules.Categories,
            cancellationToken: cancellationToken);

        InvalidateCategoryCache(id);

        _logger.LogInformation("Updated category {CategoryId}", id);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryForUpdateAsync(id, cancellationToken);
        category.SoftDelete(_currentUserService.UserId, _dateTimeProvider.UtcNow);

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Delete,
            $"Deleted category: {category.Code} - {category.Name}",
            AuditLogConstants.Modules.Categories,
            cancellationToken: cancellationToken);

        InvalidateCategoryCache(id);

        _logger.LogInformation("Soft-deleted category {CategoryId}", id);
    }

    public async Task ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryForUpdateAsync(id, cancellationToken);

        try
        {
            category.Activate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(
                CategoryErrors.AlreadyActive,
                $"Category '{category.Code}' is already active.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Activate,
            $"Activated category: {category.Code} - {category.Name}",
            AuditLogConstants.Modules.Categories,
            cancellationToken: cancellationToken);

        InvalidateCategoryCache(id);

        _logger.LogInformation("Activated category {CategoryId}", id);
    }

    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await GetCategoryForUpdateAsync(id, cancellationToken);

        try
        {
            category.Deactivate();
        }
        catch (BuildingBlocks.Domain.Exceptions.DomainException)
        {
            throw new ConflictException(
                CategoryErrors.AlreadyInactive,
                $"Category '{category.Code}' is already inactive.");
        }

        await _activityLogService.EnqueuePostCommitAsync(
            ActivityTypes.Deactivate,
            $"Deactivated category: {category.Code} - {category.Name}",
            AuditLogConstants.Modules.Categories,
            cancellationToken: cancellationToken);

        InvalidateCategoryCache(id);

        _logger.LogInformation("Deactivated category {CategoryId}", id);
    }

    public async Task<CategoryDetailResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeys.CategoryDetail(id);
        var cached = await _cacheService.GetAsync<CategoryDetailResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var category = await _unitOfWork.Repository<Category, Guid>()
            .QueryReadOnly()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            return null;
        }

        var detail = MapDetail(category);
        await _cacheService.SetAsync(cacheKey, detail, cancellationToken: cancellationToken);
        return detail;
    }

    public async Task<PagedResult<CategoryListItemResponse>> GetListAsync(
        string? keyword,
        bool? isActive,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var queryHash = CacheKeys.HashQueryParameters(keyword, isActive, pageIndex, pageSize);
        var cacheKey = CacheKeys.CategoryList(queryHash);

        return await _cacheService.GetOrSetAsync(
            cacheKey,
            async ct =>
            {
                var pagedRequest = new PagedRequest { PageIndex = pageIndex, PageSize = pageSize };
                var query = _unitOfWork.Repository<Category, Guid>()
                    .QueryReadOnly()
                    .Where(c => !c.IsDeleted);

                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    var pattern = $"%{keyword.Trim()}%";
                    query = query.Where(c =>
                        EF.Functions.ILike(c.Code, pattern) ||
                        EF.Functions.ILike(c.Name, pattern) ||
                        EF.Functions.ILike(c.Description ?? string.Empty, pattern));
                }

                if (isActive.HasValue)
                {
                    query = query.Where(c => c.IsActive == isActive.Value);
                }

                var projected = query
                    .OrderBy(c => c.SortOrder)
                    .ThenByDescending(c => c.CreatedAt)
                    .Select(c => new CategoryListItemResponse
                    {
                        Id = c.Id,
                        Code = c.Code,
                        Name = c.Name,
                        Description = c.Description,
                        SortOrder = c.SortOrder,
                        IsActive = c.IsActive,
                        CreatedAt = c.CreatedAt
                    });

                return await projected.ToPagedResultAsync(pagedRequest, ct);
            },
            cancellationToken: cancellationToken);
    }

    private async Task<Category> GetCategoryForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Repository<Category, Guid>()
            .Query()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken);

        if (category is null)
        {
            throw new NotFoundException(
                CategoryErrors.NotFound,
                $"Category with id '{id}' was not found.");
        }

        return category;
    }

    private async Task<bool> CodeExistsAsync(
        string code,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Category, Guid>()
            .QueryReadOnly()
            .Where(c => !c.IsDeleted && c.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    private static CategoryDetailResponse MapDetail(Category category) =>
        new()
        {
            Id = category.Id,
            Code = category.Code,
            Name = category.Name,
            Description = category.Description,
            SortOrder = category.SortOrder,
            IsActive = category.IsActive,
            CreatedAt = category.CreatedAt,
            CreatedBy = category.CreatedBy,
            UpdatedAt = category.UpdatedAt,
            UpdatedBy = category.UpdatedBy
        };

    private void InvalidateCategoryCache(Guid id)
    {
        _cacheInvalidationBuffer.EnqueueRemove(CacheKeys.CategoryDetail(id));
        InvalidateCategoryListCache();
    }

    private void InvalidateCategoryListCache() =>
        _cacheInvalidationBuffer.EnqueueRemoveByPrefix(CacheKeys.CategoryListPrefix);
}
