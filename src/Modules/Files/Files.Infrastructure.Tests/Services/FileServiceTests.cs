using BuildingBlocks.Application.Caching;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Testing.Fakes;
using Files.Application.Options;
using Files.Infrastructure.Persistence;
using Files.Infrastructure.Services;
using Files.Infrastructure.Storage;
using Files.Infrastructure.Tests.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Files.Infrastructure.Tests.Services;

public sealed class FileServiceTests : IDisposable
{
    private readonly FilesDbContext _dbContext;
    private readonly FilesUnitOfWork _unitOfWork;
    private readonly FakeFileStorageProvider _storage;
    private readonly FakeActivityLogService _activityLog;
    private readonly RecordingCacheService _cacheService;
    private readonly RecordingCacheInvalidationBuffer _cacheInvalidation;
    private readonly FileService _service;

    public FileServiceTests()
    {
        var currentUser = TestDataFactory.CreateCurrentUser();
        var dateTime = TestDataFactory.CreateFixedClock();

        var options = new DbContextOptionsBuilder<FilesDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        _dbContext = new FilesDbContext(options, currentUser, dateTime);
        _unitOfWork = new FilesUnitOfWork(_dbContext);
        _storage = new FakeFileStorageProvider();
        _activityLog = new FakeActivityLogService();
        _cacheService = new RecordingCacheService();
        _cacheInvalidation = new RecordingCacheInvalidationBuffer();

        var compensationBuffer = new FileStorageCompensationBuffer(
            _storage,
            NullLogger<FileStorageCompensationBuffer>.Instance);

        _service = new FileService(
            _unitOfWork,
            _storage,
            compensationBuffer,
            Options.Create(new FileStorageOptions
            {
                MaxFileSizeMb = 20,
                AllowedExtensions = [".pdf"],
                AllowedContentTypes = ["application/pdf"]
            }),
            currentUser,
            dateTime,
            _activityLog,
            _cacheService,
            _cacheInvalidation,
            NullLogger<FileService>.Instance);
    }

    [Fact]
    public async Task UploadAsync_WhenValidFile_SavesMetadata()
    {
        await using var stream = new MemoryStream("%PDF-1.4\n"u8.ToArray());

        var response = await _service.UploadAsync(
            stream,
            "document.pdf",
            "application/pdf",
            8,
            "Files",
            null,
            null,
            null,
            false);

        await _unitOfWork.SaveChangesAsync();

        var saved = await _dbContext.FileResources.SingleAsync(f => f.Id == response.Id);
        Assert.Equal("document.pdf", saved.OriginalFileName);
        Assert.False(saved.IsTemporary);
    }

    [Fact]
    public async Task DownloadAsync_WhenFileMissing_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.DownloadAsync(Guid.NewGuid()));

        Assert.Equal(FileErrors.NotFound, exception.Code);
    }

    [Fact]
    public async Task MarkAsPermanent_WhenAlreadyPermanent_DoesNotEnqueueDuplicateActivityLog()
    {
        await using var stream = new MemoryStream("%PDF-1.4\n"u8.ToArray());
        var upload = await _service.UploadAsync(
            stream,
            "document.pdf",
            "application/pdf",
            8,
            null,
            null,
            null,
            null,
            true);
        await _unitOfWork.SaveChangesAsync();
        _activityLog.PostCommitEntries.Clear();

        await _service.MarkAsPermanentAsync(upload.Id);
        await _unitOfWork.SaveChangesAsync();
        _activityLog.PostCommitEntries.Clear();

        await _service.MarkAsPermanentAsync(upload.Id);

        Assert.Empty(_activityLog.PostCommitEntries);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesMetadataAndInvalidatesCache()
    {
        await using var stream = new MemoryStream("%PDF-1.4\n"u8.ToArray());
        var upload = await _service.UploadAsync(
            stream,
            "document.pdf",
            "application/pdf",
            8,
            null,
            null,
            null,
            null,
            false);
        await _unitOfWork.SaveChangesAsync();
        _cacheInvalidation.RemovedKeys.Clear();

        await _service.DeleteAsync(upload.Id);
        await _unitOfWork.SaveChangesAsync();

        var deleted = await _dbContext.FileResources.SingleAsync(f => f.Id == upload.Id);
        Assert.True(deleted.IsDeleted);
        Assert.Contains(CacheKeys.FileResourceDetail(upload.Id), _cacheInvalidation.RemovedKeys);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotCacheBinaryContent()
    {
        await using var stream = new MemoryStream("%PDF-1.4\n"u8.ToArray());
        var upload = await _service.UploadAsync(
            stream,
            "document.pdf",
            "application/pdf",
            8,
            null,
            null,
            null,
            null,
            false);
        await _unitOfWork.SaveChangesAsync();

        await _service.GetByIdAsync(upload.Id);

        var cachedEntry = Assert.Single(_cacheService.SetOperations);
        Assert.IsType<Files.Application.Files.FileResourceDetailResponse>(cachedEntry.Value);
    }

    public void Dispose() => _dbContext.Dispose();
}
