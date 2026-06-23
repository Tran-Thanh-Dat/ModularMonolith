using AuditLogs.Infrastructure.Persistence;
using BackgroundJobs.Infrastructure.Persistence;
using Categories.Infrastructure.Persistence;
using Files.Infrastructure.Persistence;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notifications.Infrastructure.Persistence;

namespace ApiHost.Startup;

internal static class DatabaseMigrationStartup
{
    public static bool ShouldApplyMigrations(IConfiguration configuration) =>
        configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup");

    public static bool ShouldFailStartupOnMigrationError(IConfiguration configuration) =>
        configuration.GetValue("Database:FailStartupOnMigrationError", true);

    public static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
    {
        var failOnError = ShouldFailStartupOnMigrationError(app.Configuration);

        using var scope = app.Services.CreateScope();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("IdentityStartup"),
            "Identity migration/seed",
            async () =>
            {
                var identityDbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await identityDbContext.Database.MigrateAsync();

                var seeder = scope.ServiceProvider.GetRequiredService<Identity.Application.Abstractions.IIdentitySeeder>();
                await seeder.SeedAsync();
            },
            "Identity migration/seed skipped. Ensure PostgreSQL is running and run dotnet ef database update if needed.");

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("AuditLogsStartup"),
            "AuditLogs migration",
            async () =>
            {
                var auditLogsDbContext = scope.ServiceProvider.GetRequiredService<AuditLogsDbContext>();
                await auditLogsDbContext.Database.MigrateAsync();
            },
            "AuditLogs migration failed. Run: dotnet ef database update --project src/Modules/AuditLogs/AuditLogs.Infrastructure --startup-project src/ApiHost");

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("CategoriesStartup"),
            "Categories migration",
            async () =>
            {
                var categoriesDbContext = scope.ServiceProvider.GetRequiredService<CategoriesDbContext>();
                await categoriesDbContext.Database.MigrateAsync();
            },
            "Categories migration failed. Run: dotnet ef database update --project src/Modules/Categories/Categories.Infrastructure --startup-project src/ApiHost --context CategoriesDbContext");

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("FilesStartup"),
            "Files migration",
            async () =>
            {
                var filesDbContext = scope.ServiceProvider.GetRequiredService<FilesDbContext>();
                await filesDbContext.Database.MigrateAsync();
            },
            "Files migration failed. Run: dotnet ef database update --project src/Modules/Files/Files.Infrastructure --startup-project src/ApiHost --context FilesDbContext");

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("NotificationsStartup"),
            "Notifications migration",
            async () =>
            {
                var notificationsDbContext = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
                await notificationsDbContext.Database.MigrateAsync();
            },
            "Notifications migration failed. Run: dotnet ef database update --project src/Modules/Notifications/Notifications.Infrastructure --startup-project src/ApiHost --context NotificationsDbContext");

        await RunStepAsync(
            failOnError,
            loggerFactory.CreateLogger("BackgroundJobsStartup"),
            "BackgroundJobs migration",
            async () =>
            {
                var backgroundJobsDbContext = scope.ServiceProvider.GetRequiredService<BackgroundJobsDbContext>();
                await backgroundJobsDbContext.Database.MigrateAsync();
            },
            "BackgroundJobs migration failed. Run: dotnet ef database update --project src/Modules/BackgroundJobs/BackgroundJobs.Infrastructure --startup-project src/ApiHost --context BackgroundJobsDbContext");
    }

    private static async Task RunStepAsync(
        bool failOnError,
        ILogger logger,
        string stepName,
        Func<Task> action,
        string continueMessage)
    {
        try
        {
            await action();
            logger.LogInformation("{StepName} completed successfully.", stepName);
        }
        catch (Exception exception)
        {
            if (failOnError)
            {
                logger.LogCritical(exception, "{StepName} failed. Startup is aborting.", stepName);
                throw new InvalidOperationException($"{stepName} failed. See inner exception for details.", exception);
            }

            logger.LogWarning(exception, "{ContinueMessage}", continueMessage);
        }
    }
}
