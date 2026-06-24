using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Domain.AuthorizationMatrix;
using AuthorizationPolicies.Domain.Constants;
using AuthorizationPolicies.Domain.Enums;
using AuthorizationPolicies.Infrastructure.Persistence;
using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthorizationPolicies.Infrastructure.Seeding;

public sealed class AuthorizationPoliciesSeeder : IAuthorizationPoliciesSeeder
{
    private readonly AuthorizationPoliciesDbContext _dbContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<AuthorizationPoliciesSeeder> _logger;

    public AuthorizationPoliciesSeeder(
        AuthorizationPoliciesDbContext dbContext,
        IDateTimeProvider dateTimeProvider,
        ILogger<AuthorizationPoliciesSeeder> logger)
    {
        _dbContext = dbContext;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedMatrixBaselineAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("AuthorizationPolicies seed completed.");
    }

    private async Task SeedMatrixBaselineAsync(CancellationToken cancellationToken)
    {
        var entries = new (string Module, string Resource, string Action, AuthorizationScope Scope, string Permission)[]
        {
            ("Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.View, AuthorizationScope.Global, "Tenant.View"),
            ("Organizations", AuthorizationResourceTypes.Tenant, AuthorizationActions.Manage, AuthorizationScope.Global, "Tenant.Manage"),
            ("Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.View, AuthorizationScope.Organization, "Organization.View"),
            ("Organizations", AuthorizationResourceTypes.Organization, AuthorizationActions.Manage, AuthorizationScope.Organization, "Organization.Manage"),
            ("Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.View, AuthorizationScope.Workspace, "Workspace.View"),
            ("Organizations", AuthorizationResourceTypes.Workspace, AuthorizationActions.Manage, AuthorizationScope.Workspace, "Workspace.Manage"),
            ("MasterData", "MasterDataGroup", AuthorizationActions.View, AuthorizationScope.Global, "MasterDataGroup.View"),
            ("MasterData", "MasterDataGroup", AuthorizationActions.Manage, AuthorizationScope.Global, "MasterDataGroup.Manage"),
            ("MasterData", "MasterDataItem", AuthorizationActions.View, AuthorizationScope.Global, "MasterDataItem.View"),
            ("MasterData", "MasterDataItem", AuthorizationActions.Manage, AuthorizationScope.Global, "MasterDataItem.Manage"),
            ("MasterData", "Lookup", AuthorizationActions.View, AuthorizationScope.Global, "Lookup.View"),
            ("MasterData", "Lookup", AuthorizationActions.View, AuthorizationScope.Tenant, "Lookup.View"),
            ("MasterData", "Lookup", AuthorizationActions.View, AuthorizationScope.Organization, "Lookup.View"),
            ("AsyncTasks", "AsyncTask", AuthorizationActions.View, AuthorizationScope.OwnerOnly, "AsyncTask.View"),
            ("AsyncTasks", "AsyncTask", AuthorizationActions.Create, AuthorizationScope.Tenant, "AsyncTask.Submit"),
            ("AsyncTasks", "AsyncTask", AuthorizationActions.Cancel, AuthorizationScope.OwnerOnly, "AsyncTask.Cancel"),
            ("AsyncTasks", "AsyncTask", AuthorizationActions.Execute, AuthorizationScope.OwnerOnly, "AsyncTask.Retry"),
            ("AsyncTasks", "AsyncTask", AuthorizationActions.Manage, AuthorizationScope.Global, "AsyncTask.Manage")
        };

        foreach (var (module, resource, action, scope, permission) in entries)
        {
            var exists = await _dbContext.AuthorizationMatrixEntries
                .AnyAsync(e => !e.IsDeleted && e.ModuleCode == module && e.ResourceType == resource &&
                               e.Action == action && e.Scope == scope, cancellationToken);

            if (exists)
            {
                continue;
            }

            var entry = AuthorizationMatrixEntry.Create(
                module,
                resource,
                action,
                scope,
                permission,
                $"Baseline matrix for {resource} {action}",
                null,
                _dateTimeProvider.UtcNow);

            await _dbContext.AuthorizationMatrixEntries.AddAsync(entry, cancellationToken);
            _logger.LogInformation("Seeded matrix entry {Module}/{Resource}/{Action}", module, resource, action);
        }
    }
}
