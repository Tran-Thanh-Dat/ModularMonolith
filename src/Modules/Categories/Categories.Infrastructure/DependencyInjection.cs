using BuildingBlocks.Application.Abstractions;
using Categories.Application.Abstractions;
using Categories.Infrastructure.Persistence;
using Categories.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Categories.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<CategoriesDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "categories"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        // Register concrete UoW for typed injection in Categories services.
        // Also register as IUnitOfWork so TransactionBehavior receives it via IEnumerable<IUnitOfWork>.
        services.AddScoped<CategoriesUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CategoriesUnitOfWork>());
        services.AddScoped<ICategoryService, CategoryService>();

        return services;
    }
}
