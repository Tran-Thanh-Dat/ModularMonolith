using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using Categories.Domain.Categories;
using Categories.Domain.Constants;
using Categories.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Categories.Infrastructure.Persistence;

public sealed class CategoriesDbContext : AuditableDbContext
{
    public CategoriesDbContext(
        DbContextOptions<CategoriesDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(CategoriesConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CategoryConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class CategoriesUnitOfWork : EfUnitOfWork<CategoriesDbContext>
{
    private readonly ILogger<CategoriesUnitOfWork> _logger;

    public CategoriesUnitOfWork(
        CategoriesDbContext dbContext,
        ILogger<CategoriesUnitOfWork> logger)
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
        catch (DbUpdateException exception) when (IsCategoryCodeUniqueViolation(exception))
        {
            _logger.LogWarning(
                exception,
                "Category code unique constraint violation on save.");

            throw new ConflictException(
                CategoryErrors.CodeAlreadyExists,
                "Category code already exists.");
        }
    }

    private static bool IsCategoryCodeUniqueViolation(DbUpdateException exception)
    {
        var postgres = FindPostgresException(exception);
        if (postgres is null || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return postgres.ConstraintName?.Contains("Code", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static PostgresException? FindPostgresException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }
}
