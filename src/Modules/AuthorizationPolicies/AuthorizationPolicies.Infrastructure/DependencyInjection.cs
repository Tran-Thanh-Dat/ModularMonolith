using AuthorizationPolicies.Application.Abstractions;
using AuthorizationPolicies.Infrastructure.Persistence;
using AuthorizationPolicies.Infrastructure.Seeding;
using AuthorizationPolicies.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationPolicies.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationPoliciesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuthorizationPoliciesDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "authorization"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<AuthorizationPoliciesUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AuthorizationPoliciesUnitOfWork>());

        services.AddScoped<PermissionPolicyService>();
        services.AddScoped<IPermissionPolicyService>(sp => sp.GetRequiredService<PermissionPolicyService>());

        services.AddScoped<RolePermissionPolicyService>();
        services.AddScoped<IRolePermissionPolicyService>(sp => sp.GetRequiredService<RolePermissionPolicyService>());

        services.AddScoped<UserPermissionPolicyOverrideService>();
        services.AddScoped<IUserPermissionPolicyOverrideService>(sp => sp.GetRequiredService<UserPermissionPolicyOverrideService>());

        services.AddScoped<AuthorizationMatrixEntryService>();
        services.AddScoped<IAuthorizationMatrixEntryService>(sp => sp.GetRequiredService<AuthorizationMatrixEntryService>());

        services.AddScoped<CurrentUserPermissionContextService>();
        services.AddScoped<ICurrentUserPermissionContextService>(sp => sp.GetRequiredService<CurrentUserPermissionContextService>());

        services.AddScoped<AuthorizationMatrixService>();
        services.AddScoped<IAuthorizationMatrixService>(sp => sp.GetRequiredService<AuthorizationMatrixService>());

        services.AddScoped<AuthorizationCheckRequestValidator>();
        services.AddScoped<IAuthorizationCheckRequestValidator>(sp => sp.GetRequiredService<AuthorizationCheckRequestValidator>());

        services.AddScoped<IAuthorizationPoliciesSeeder, AuthorizationPoliciesSeeder>();

        return services;
    }
}
