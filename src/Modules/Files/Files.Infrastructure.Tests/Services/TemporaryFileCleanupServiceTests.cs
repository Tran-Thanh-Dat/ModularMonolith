using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Files.Domain.FileResources;
using Files.Infrastructure.Persistence;
using Files.Infrastructure.Services;
using Files.Infrastructure.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Files.Infrastructure.Tests.Services;

public sealed class TemporaryFileCleanupServiceTests : IDisposable
{
    private readonly FilesDbContext _dbContext;
    private readonly FilesUnitOfWork _unitOfWork;
    private readonly FakeFileStorageProvider _storage;
    private readonly FixedDateTimeProvider _dateTime;
    private readonly TemporaryFileCleanupService _service;

    public TemporaryFileCleanupServiceTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        _dateTime = TestDataFactory.CreateFixedClock(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new FilesDbContext(options, currentUser, _dateTime);
        _unitOfWork = new FilesUnitOfWork(_dbContext);
        _storage = new FakeFileStorageProvider();
        _service = new TemporaryFileCleanupService(
            _unitOfWork,
            _storage,
            _dateTime,
            new FakeActivityLogService(),
            NullLogger<TemporaryFileCleanupService>.Instance);
    }

    [Fact]
    public async Task ProcessBatchAsync_PicksOnlyExpiredTemporaryFiles()
    {
        await SeedFile(isTemporary: true, expiresAt: _dateTime.UtcNow.AddHours(-1));
        await SeedFile(isTemporary: true, expiresAt: _dateTime.UtcNow.AddHours(2));
        await _dbContext.SaveChangesAsync();

        var result = await _service.ProcessBatchAsync(batchSize: 10, deletePhysicalFiles: false);

        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.SuccessCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_DoesNotTouchPermanentFiles()
    {
        await SeedFile(isTemporary: false, expiresAt: _dateTime.UtcNow.AddHours(-1));
        await _dbContext.SaveChangesAsync();

        var result = await _service.ProcessBatchAsync(batchSize: 10, deletePhysicalFiles: false);

        Assert.Equal(0, result.ProcessedCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_SoftDeletesMetadata()
    {
        var expired = await SeedFile(isTemporary: true, expiresAt: _dateTime.UtcNow.AddHours(-1));
        await _dbContext.SaveChangesAsync();

        await _service.ProcessBatchAsync(batchSize: 10, deletePhysicalFiles: false);
        await _dbContext.SaveChangesAsync();

        var updated = await _dbContext.FileResources.SingleAsync(f => f.Id == expired.Id);
        Assert.True(updated.IsDeleted);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenConfigured_DeletesPhysicalFile()
    {
        var expired = await SeedFile(isTemporary: true, expiresAt: _dateTime.UtcNow.AddHours(-1));
        await _dbContext.SaveChangesAsync();

        await _service.ProcessBatchAsync(batchSize: 10, deletePhysicalFiles: true);
        await _dbContext.SaveChangesAsync();

        Assert.Contains(expired.StoragePath, _storage.DeletedPaths);
    }

    private async Task<FileResource> SeedFile(bool isTemporary, DateTimeOffset? expiresAt)
    {
        var file = FileResource.Create(
            "document.pdf",
            "stored.pdf",
            ".pdf",
            "application/pdf",
            100,
            "Fake",
            "uploads/stored.pdf",
            null,
            "checksum",
            null,
            null,
            null,
            null,
            isTemporary,
            expiresAt,
            _dateTime.UtcNow,
            TestDataFactory.DefaultUserId);

        await _dbContext.FileResources.AddAsync(file);
        return file;
    }

    public void Dispose() => _dbContext.Dispose();
}
