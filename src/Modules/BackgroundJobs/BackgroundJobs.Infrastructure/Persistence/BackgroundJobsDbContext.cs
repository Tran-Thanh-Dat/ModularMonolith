using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Infrastructure.Persistence;
using BackgroundJobs.Domain.Constants;
using BackgroundJobs.Domain.JobExecutions;
using BackgroundJobs.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace BackgroundJobs.Infrastructure.Persistence;

public sealed class BackgroundJobsDbContext : AuditableDbContext
{
    public BackgroundJobsDbContext(
        DbContextOptions<BackgroundJobsDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<BackgroundJobExecution> JobExecutions => Set<BackgroundJobExecution>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(BackgroundJobsConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BackgroundJobExecutionConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class BackgroundJobsUnitOfWork : EfUnitOfWork<BackgroundJobsDbContext>
{
    public BackgroundJobsUnitOfWork(BackgroundJobsDbContext dbContext)
        : base(dbContext)
    {
    }
}
