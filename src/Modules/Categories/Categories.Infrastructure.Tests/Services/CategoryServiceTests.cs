using AuditLogs.Domain.Constants;
using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Categories.Domain.Categories;
using Categories.Infrastructure.Persistence;
using Categories.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Categories.Infrastructure.Tests.Services;

public sealed class CategoryServiceTests : IDisposable
{
    private readonly CategoriesDbContext _dbContext;
    private readonly CategoriesUnitOfWork _unitOfWork;
    private readonly FakeActivityLogService _activityLog;
    private readonly RecordingCacheInvalidationBuffer _cacheInvalidation;
    private readonly RecordingCacheService _cacheService;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var currentUser = new FakeCurrentUserService();
        var dateTime = new FixedDateTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var options = new DbContextOptionsBuilder<CategoriesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new CategoriesDbContext(options, currentUser, dateTime);
        _unitOfWork = new CategoriesUnitOfWork(_dbContext, NullLogger<CategoriesUnitOfWork>.Instance);
        _activityLog = new FakeActivityLogService();
        _cacheInvalidation = new RecordingCacheInvalidationBuffer();
        _cacheService = new RecordingCacheService();

        _service = new CategoryService(
            _unitOfWork,
            currentUser,
            dateTime,
            _activityLog,
            _cacheService,
            _cacheInvalidation,
            NullLogger<CategoryService>.Instance);
    }

    [Fact]
    public async Task CreateCategory_WhenCodeIsUnique_ReturnsNewIdAndEnqueuesActivityAndCacheInvalidation()
    {
        var id = await _service.CreateAsync("CAT-001", "Category One", "Desc", 1);
        await _unitOfWork.SaveChangesAsync();

        var created = await _dbContext.Categories.SingleAsync(c => c.Id == id);
        Assert.Equal("CAT-001", created.Code);
        Assert.Single(_activityLog.PostCommitEntries);
        Assert.Contains(CacheKeys.CategoryListPrefix, _cacheInvalidation.RemovedPrefixes);
    }

    [Fact]
    public async Task CreateCategory_WhenCodeExists_ReturnsCodeAlreadyExists()
    {
        await _service.CreateAsync("CAT-001", "First", null, 1);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateAsync("CAT-001", "Second", null, 2));

        Assert.Equal(CategoryErrors.CodeAlreadyExists, exception.Code);
    }

    [Fact]
    public async Task UpdateCategory_DoesNotChangeCode()
    {
        var id = await _service.CreateAsync("CAT-001", "Original", null, 1);
        await _unitOfWork.SaveChangesAsync();

        await _service.UpdateAsync(id, "Updated Name", "Updated desc", 5);
        await _unitOfWork.SaveChangesAsync();

        var updated = await _dbContext.Categories.SingleAsync(c => c.Id == id);
        Assert.Equal("CAT-001", updated.Code);
        Assert.Equal("Updated Name", updated.Name);
    }

    [Fact]
    public async Task ActivateCategory_WhenAlreadyActive_ReturnsAlreadyActive()
    {
        var id = await _service.CreateAsync("CAT-001", "Active", null, 1);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.ActivateAsync(id));

        Assert.Equal(CategoryErrors.AlreadyActive, exception.Code);
    }

    [Fact]
    public async Task DeactivateCategory_WhenAlreadyInactive_ReturnsAlreadyInactive()
    {
        var id = await _service.CreateAsync("CAT-001", "Active", null, 1);
        await _unitOfWork.SaveChangesAsync();
        await _service.DeactivateAsync(id);
        await _unitOfWork.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            _service.DeactivateAsync(id));

        Assert.Equal(CategoryErrors.AlreadyInactive, exception.Code);
    }

    [Fact]
    public async Task DeleteCategory_ExcludesCategoryFromDetail()
    {
        var id = await _service.CreateAsync("CAT-001", "To Delete", null, 1);
        await _unitOfWork.SaveChangesAsync();

        await _service.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        var detail = await _service.GetByIdAsync(id);
        Assert.Null(detail);
    }

    [Fact]
    public async Task GetByIdAsync_UsesCacheOnSecondRead()
    {
        var id = await _service.CreateAsync("CAT-001", "Cached", null, 1);
        await _unitOfWork.SaveChangesAsync();

        await _service.GetByIdAsync(id);
        var setCountAfterFirst = _cacheService.SetOperations.Count;

        await _service.GetByIdAsync(id);

        Assert.Equal(setCountAfterFirst, _cacheService.SetOperations.Count);
    }

    [Fact]
    public async Task GetListAsync_ReturnsPagedResults()
    {
        for (var i = 1; i <= 5; i++)
        {
            await _service.CreateAsync($"CAT-{i:D3}", $"Category {i}", null, i);
        }

        await _unitOfWork.SaveChangesAsync();

        var page = await _service.GetListAsync(null, null, 1, 2);

        Assert.Equal(2, page.Items.Count);
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(2, page.PageSize);
    }

    [Fact]
    public async Task UpdateCategory_InvalidatesDetailAndListCache()
    {
        var id = await _service.CreateAsync("CAT-001", "Original", null, 1);
        await _unitOfWork.SaveChangesAsync();
        _cacheInvalidation.RemovedPrefixes.Clear();
        _cacheInvalidation.RemovedKeys.Clear();

        await _service.UpdateAsync(id, "Updated", "Desc", 2);
        await _unitOfWork.SaveChangesAsync();

        Assert.Contains(CacheKeys.CategoryListPrefix, _cacheInvalidation.RemovedPrefixes);
        Assert.Contains(CacheKeys.CategoryDetail(id), _cacheInvalidation.RemovedKeys);
    }

    [Fact]
    public async Task DeleteCategory_InvalidatesDetailAndListCache()
    {
        var id = await _service.CreateAsync("CAT-001", "Delete Me", null, 1);
        await _unitOfWork.SaveChangesAsync();
        _cacheInvalidation.RemovedPrefixes.Clear();
        _cacheInvalidation.RemovedKeys.Clear();

        await _service.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();

        Assert.Contains(CacheKeys.CategoryListPrefix, _cacheInvalidation.RemovedPrefixes);
        Assert.Contains(CacheKeys.CategoryDetail(id), _cacheInvalidation.RemovedKeys);
    }

    [Fact(Skip = "EF InMemory does not support ILike; keyword search requires PostgreSQL integration test.")]
    public async Task GetListAsync_WithKeyword_FiltersResults()
    {
        await _service.CreateAsync("CAT-001", "Alpha", null, 1);
        await _service.CreateAsync("CAT-002", "Beta", null, 2);
        await _unitOfWork.SaveChangesAsync();

        var page = await _service.GetListAsync("Alpha", null, 1, 20);

        Assert.Single(page.Items);
        Assert.Equal("Alpha", page.Items.First().Name);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
