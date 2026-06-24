using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.Errors;
using BuildingBlocks.Application.Errors;
using AuthorizationPolicies.Domain.PermissionPolicies;
using AuthorizationPolicies.Domain.RolePermissionPolicies;
using AuthorizationPolicies.Domain.UserPermissionPolicyOverrides;
using AuthorizationPolicies.Infrastructure.Persistence.Configurations;
using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Errors;
using BuildingBlocks.Application.Exceptions;
using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AuthorizationPolicies.Infrastructure.Persistence;

public sealed class AuthorizationPoliciesDbContext : AuditableDbContext
{
    public AuthorizationPoliciesDbContext(
        DbContextOptions<AuthorizationPoliciesDbContext> options,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
        : base(options, currentUserService, dateTimeProvider)
    {
    }

    public DbSet<PermissionPolicy> PermissionPolicies => Set<PermissionPolicy>();
    public DbSet<RolePermissionPolicy> RolePermissionPolicies => Set<RolePermissionPolicy>();
    public DbSet<UserPermissionPolicyOverride> UserPermissionPolicyOverrides => Set<UserPermissionPolicyOverride>();
    public DbSet<AuthorizationMatrixEntry> AuthorizationMatrixEntries => Set<AuthorizationMatrixEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(AuthorizationPoliciesConstants.SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PermissionPolicyConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}

public sealed class AuthorizationPoliciesUnitOfWork : EfUnitOfWork<AuthorizationPoliciesDbContext>
{
    private readonly ILogger<AuthorizationPoliciesUnitOfWork> _logger;

    public AuthorizationPoliciesUnitOfWork(
        AuthorizationPoliciesDbContext dbContext,
        ILogger<AuthorizationPoliciesUnitOfWork> logger)
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
            _logger.LogWarning(exception, "AuthorizationPolicies unique constraint violation on save.");
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
        if (constraint.Contains("permission_policies", StringComparison.OrdinalIgnoreCase) &&
            constraint.Contains("Code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = PermissionPolicyErrors.CodeAlreadyExists;
            message = "Permission policy code already exists.";
            return true;
        }

        if (constraint.Contains("role_permission_policies", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = RolePermissionPolicyErrors.AlreadyExists;
            message = "Role permission policy assignment already exists.";
            return true;
        }

        if (constraint.Contains("user_permission_policy_overrides", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = UserPermissionPolicyOverrideErrors.AlreadyExists;
            message = "User permission policy override already exists.";
            return true;
        }

        if (constraint.Contains("authorization_matrix_entries", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = AuthorizationMatrixErrors.AlreadyExists;
            message = "Authorization matrix entry already exists.";
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
