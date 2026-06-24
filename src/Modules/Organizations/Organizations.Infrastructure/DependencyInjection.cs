using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Organizations.Application.Abstractions;
using Microsoft.Extensions.Options;
using Organizations.Infrastructure.Persistence;
using Organizations.Infrastructure.Seeding;
using Organizations.Infrastructure.Services;

namespace Organizations.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrganizationsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "organizations"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<OrganizationsUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrganizationsUnitOfWork>());
        services.AddScoped<TenantService>();
        services.AddScoped<ITenantService>(sp => sp.GetRequiredService<TenantService>());
        services.AddScoped<OrganizationService>();
        services.AddScoped<IOrganizationService>(sp => sp.GetRequiredService<OrganizationService>());
        services.AddScoped<OrganizationUserService>();
        services.AddScoped<IOrganizationUserService>(sp => sp.GetRequiredService<OrganizationUserService>());
        services.AddScoped<WorkspaceService>();
        services.AddScoped<IWorkspaceService>(sp => sp.GetRequiredService<WorkspaceService>());
        services.AddScoped<WorkspaceUserService>();
        services.AddScoped<IWorkspaceUserService>(sp => sp.GetRequiredService<WorkspaceUserService>());

        services.Configure<OrganizationsSeedOptions>(configuration.GetSection(OrganizationsSeedOptions.SectionName));
        services.AddScoped<IOrganizationsSeeder, OrganizationsSeeder>();

        return services;
    }
}
