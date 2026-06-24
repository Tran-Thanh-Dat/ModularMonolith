using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Domain.Entities;
using AsyncTasks.Domain.Errors;
using AsyncTasks.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AsyncTasks.Infrastructure.Persistence;

public sealed class AsyncTasksDbContext : AuditableDbContext
{
    public AsyncTasksDbContext(
        DbContextOptions<AsyncTasksDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<AsyncTask> AsyncTasks => Set<AsyncTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AsyncTasksConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AsyncTaskConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class AsyncTasksUnitOfWork : EfUnitOfWork<AsyncTasksDbContext>
{
    private readonly ILogger<AsyncTasksUnitOfWork> _logger;

    public AsyncTasksUnitOfWork(AsyncTasksDbContext dbContext, ILogger<AsyncTasksUnitOfWork> logger)
        : base(dbContext)
    {
        _logger = logger;
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsTaskNoUniqueViolation(exception))
        {
            _logger.LogWarning(exception, "AsyncTask task number unique constraint violation.");
            throw new ConflictException(AsyncTaskErrors.TaskNoAlreadyExists, "Task number already exists.");
        }
    }

    private static bool IsTaskNoUniqueViolation(DbUpdateException exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres &&
                postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
                (postgres.ConstraintName?.Contains("task_no", StringComparison.OrdinalIgnoreCase) == true ||
                 postgres.ConstraintName?.Contains("async_tasks", StringComparison.OrdinalIgnoreCase) == true))
            {
                return true;
            }
        }

        return false;
    }
}
