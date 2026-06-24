using BuildingBlocks.Application.Abstractions;
using MasterData.Application.Abstractions;
using MasterData.Infrastructure.Persistence;
using MasterData.Infrastructure.Seeders;
using MasterData.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MasterData.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMasterDataInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<MasterDataDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "master_data"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<MasterDataUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<MasterDataUnitOfWork>());
        services.AddScoped<MasterDataScopeValidator>();
        services.AddScoped<IMasterDataGroupService, MasterDataGroupService>();
        services.AddScoped<IMasterDataItemService, MasterDataItemService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IMasterDataSeeder, MasterDataSeeder>();

        return services;
    }
}
