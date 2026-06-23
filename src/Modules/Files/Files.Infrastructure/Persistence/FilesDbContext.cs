using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Persistence;
using Files.Domain.Constants;
using Files.Domain.FileResources;
using Files.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Files.Infrastructure.Persistence;

public sealed class FilesDbContext : AuditableDbContext
{
    public FilesDbContext(
        DbContextOptions<FilesDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<FileResource> FileResources => Set<FileResource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(FilesConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileResourceConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class FilesUnitOfWork : EfUnitOfWork<FilesDbContext>
{
    public FilesUnitOfWork(FilesDbContext dbContext)
        : base(dbContext)
    {
    }
}
