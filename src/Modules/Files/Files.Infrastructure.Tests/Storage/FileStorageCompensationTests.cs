using Files.Application.Abstractions;
using Files.Infrastructure.Tests.Fakes;
using Files.Infrastructure.Tests.Storage;
using Files.Infrastructure.Services;
using Files.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Files.Infrastructure.Tests.Storage;

public sealed class FileStorageCompensationTests
{
    [Fact]
    public async Task OnRollbackAsync_DeletesTrackedPhysicalFile()
    {
        var storage = new FakeFileStorageProvider();
        var buffer = new FileStorageCompensationBuffer(storage, NullLogger<FileStorageCompensationBuffer>.Instance);
        var hook = new FileStorageCompensationPostCommitHook(buffer);

        buffer.TrackPendingDeletion("uploads/temp/file.pdf");

        await hook.OnRollbackAsync();

        Assert.Contains("uploads/temp/file.pdf", storage.DeletedPaths);
    }

    [Fact]
    public async Task OnRollbackAsync_WhenDeleteFails_LeavesOriginalExceptionPathAvailable()
    {
        var storage = new FakeFileStorageProvider { ThrowOnDelete = true };
        var buffer = new FileStorageCompensationBuffer(storage, NullLogger<FileStorageCompensationBuffer>.Instance);
        var hook = new FileStorageCompensationPostCommitHook(buffer);

        buffer.TrackPendingDeletion("uploads/temp/file.pdf");

        var exception = await Record.ExceptionAsync(() => hook.OnRollbackAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task OnCommittedAsync_ClearsPendingPathsWithoutDeleting()
    {
        var storage = new FakeFileStorageProvider();
        var buffer = new FileStorageCompensationBuffer(storage, NullLogger<FileStorageCompensationBuffer>.Instance);
        var hook = new FileStorageCompensationPostCommitHook(buffer);

        buffer.TrackPendingDeletion("uploads/temp/file.pdf");
        await hook.OnCommittedAsync();
        await hook.OnRollbackAsync();

        Assert.Empty(storage.DeletedPaths);
    }
}
