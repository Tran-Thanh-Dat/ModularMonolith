using BuildingBlocks.Application.Abstractions;
using Files.Application.Abstractions;
using Files.Application.Options;
using Files.Infrastructure.Persistence;
using Files.Infrastructure.Services;
using Files.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Files.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<FileStorageOptions>(configuration.GetSection(FileStorageOptions.SectionName));

        services.AddDbContext<FilesDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "files"))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddSingleton<LocalFileStorageProvider>();
        services.AddSingleton<IFileStorageProvider>(provider =>
        {
            var options = configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
                          ?? new FileStorageOptions();

            return options.Provider.Equals("Local", StringComparison.OrdinalIgnoreCase)
                ? provider.GetRequiredService<LocalFileStorageProvider>()
                : throw new InvalidOperationException(
                    $"File storage provider '{options.Provider}' is not supported.");
        });

        // Register concrete UoW for typed injection in Files services.
        // Also register as IUnitOfWork so TransactionBehavior receives it via IEnumerable<IUnitOfWork>.
        services.AddScoped<FilesUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<FilesUnitOfWork>());
        services.AddScoped<IFileStorageCompensationBuffer, FileStorageCompensationBuffer>();
        services.AddScoped<IPostCommitHook, FileStorageCompensationPostCommitHook>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<ITemporaryFileCleanupService, TemporaryFileCleanupService>();

        return services;
    }
}
