using BuildingBlocks.Application.Abstractions;
using Settings.Application.Abstractions;
using Settings.Infrastructure.Persistence;
using Settings.Infrastructure.Seeding;
using Settings.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Settings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSettingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<SettingsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "settings"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<SettingsUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<SettingsUnitOfWork>());

        services.AddScoped<SettingService>();
        services.AddScoped<ISettingService>(provider => provider.GetRequiredService<SettingService>());
        services.AddScoped<ISettingProvider, SettingProvider>();
        services.AddScoped<IAccessPolicyService, AccessPolicyService>();
        services.AddScoped<IPasswordPolicyValidator, PasswordPolicyValidator>();
        services.AddScoped<ISettingsSeeder, SettingsSeeder>();

        return services;
    }
}
