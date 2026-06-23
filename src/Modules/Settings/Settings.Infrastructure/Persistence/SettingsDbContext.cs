using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Settings.Domain.Constants;
using Settings.Domain.Settings;
using Settings.Infrastructure.Persistence.Configurations;

namespace Settings.Infrastructure.Persistence;

public sealed class SettingsDbContext : AuditableDbContext
{
    public SettingsDbContext(
        DbContextOptions<SettingsDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SettingsConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SystemSettingConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class SettingsUnitOfWork : EfUnitOfWork<SettingsDbContext>
{
    private readonly ILogger<SettingsUnitOfWork> _logger;

    public SettingsUnitOfWork(
        SettingsDbContext dbContext,
        ILogger<SettingsUnitOfWork> logger)
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
        catch (DbUpdateException exception) when (IsSettingKeyUniqueViolation(exception))
        {
            _logger.LogWarning(
                exception,
                "System setting key unique constraint violation on save.");

            throw new ConflictException(
                SettingErrors.KeyAlreadyExists,
                "Setting key already exists.");
        }
    }

    private static bool IsSettingKeyUniqueViolation(DbUpdateException exception)
    {
        var postgres = FindPostgresException(exception);
        if (postgres is null || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        return postgres.ConstraintName?.Contains("Key", StringComparison.OrdinalIgnoreCase) == true;
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
