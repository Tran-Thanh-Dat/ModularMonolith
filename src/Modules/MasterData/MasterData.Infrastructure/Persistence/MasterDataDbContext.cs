using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using MasterData.Domain.Constants;
using MasterData.Domain.Entities;
using MasterData.Domain.Errors;
using MasterData.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MasterData.Infrastructure.Persistence;

public sealed class MasterDataDbContext : AuditableDbContext
{
    public MasterDataDbContext(
        DbContextOptions<MasterDataDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<MasterDataGroup> MasterDataGroups => Set<MasterDataGroup>();

    public DbSet<MasterDataItem> MasterDataItems => Set<MasterDataItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(MasterDataConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDataGroupConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class MasterDataUnitOfWork : EfUnitOfWork<MasterDataDbContext>
{
    private readonly ILogger<MasterDataUnitOfWork> _logger;

    public MasterDataUnitOfWork(MasterDataDbContext dbContext, ILogger<MasterDataUnitOfWork> logger)
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
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, out var isGroupCode, out var isItemCode))
        {
            _logger.LogWarning(exception, "MasterData unique constraint violation on save.");

            if (isGroupCode)
            {
                throw new ConflictException(
                    MasterDataGroupErrors.CodeAlreadyExists,
                    "Master data group code already exists for this scope.");
            }

            if (isItemCode)
            {
                throw new ConflictException(
                    MasterDataItemErrors.CodeAlreadyExists,
                    "Master data item code already exists in this group.");
            }

            throw;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception, out bool isGroupCode, out bool isItemCode)
    {
        isGroupCode = false;
        isItemCode = false;

        var postgres = FindPostgresException(exception);
        if (postgres is null || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        var constraint = postgres.ConstraintName ?? string.Empty;
        isGroupCode = constraint.Contains("master_data_groups", StringComparison.OrdinalIgnoreCase) ||
                      constraint.Contains("group_scope", StringComparison.OrdinalIgnoreCase);
        isItemCode = constraint.Contains("master_data_items", StringComparison.OrdinalIgnoreCase) ||
                     constraint.Contains("group_code", StringComparison.OrdinalIgnoreCase) ||
                     constraint.Contains("default", StringComparison.OrdinalIgnoreCase);

        return isGroupCode || isItemCode;
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
