using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Organizations.Domain.Constants;
using Organizations.Domain.OrganizationUsers;
using Organizations.Domain.Tenants;
using Organizations.Domain.WorkspaceUsers;
using Organizations.Domain.Workspaces;
using Organizations.Infrastructure.Persistence.Configurations;
using OrganizationEntity = Organizations.Domain.Organizations.Organization;

namespace Organizations.Infrastructure.Persistence;

public sealed class OrganizationsDbContext : AuditableDbContext
{
    public OrganizationsDbContext(
        DbContextOptions<OrganizationsDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<OrganizationUser> OrganizationUsers => Set<OrganizationUser>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceUser> WorkspaceUsers => Set<WorkspaceUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(OrganizationsConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TenantConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class OrganizationsUnitOfWork : EfUnitOfWork<OrganizationsDbContext>
{
    private readonly ILogger<OrganizationsUnitOfWork> _logger;

    public OrganizationsUnitOfWork(
        OrganizationsDbContext dbContext,
        ILogger<OrganizationsUnitOfWork> logger)
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
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, out var errorCode, out var message))
        {
            _logger.LogWarning(exception, "Organizations unique constraint violation on save.");
            throw new ConflictException(errorCode, message);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception, out string errorCode, out string message)
    {
        errorCode = CommonErrors.Conflict;
        message = "A unique constraint was violated.";

        var postgres = FindPostgresException(exception);
        if (postgres is null || postgres.SqlState != PostgresErrorCodes.UniqueViolation)
        {
            return false;
        }

        var constraint = postgres.ConstraintName ?? string.Empty;
        if (constraint.Contains("Tenant", StringComparison.OrdinalIgnoreCase) &&
            constraint.Contains("Code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = TenantErrors.CodeAlreadyExists;
            message = "Tenant code already exists.";
            return true;
        }

        if (constraint.Contains("Organization", StringComparison.OrdinalIgnoreCase) &&
            constraint.Contains("Code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = OrganizationErrors.CodeAlreadyExists;
            message = "Organization code already exists for tenant.";
            return true;
        }

        if (constraint.Contains("Workspace", StringComparison.OrdinalIgnoreCase) &&
            constraint.Contains("Code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = WorkspaceErrors.CodeAlreadyExists;
            message = "Workspace code already exists for tenant.";
            return true;
        }

        if (constraint.Contains("OrganizationUser", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = OrganizationUserErrors.AlreadyExists;
            message = "Organization membership already exists.";
            return true;
        }

        if (constraint.Contains("WorkspaceUser", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = WorkspaceUserErrors.AlreadyExists;
            message = "Workspace membership already exists.";
            return true;
        }

        return false;
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
