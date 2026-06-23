using BackgroundJobs.Application.Abstractions;
using BackgroundJobs.Application.Options;
using BackgroundJobs.Domain.Constants;
using BackgroundJobs.Infrastructure.Hangfire;
using BackgroundJobs.Infrastructure.Jobs;
using BackgroundJobs.Infrastructure.Persistence;
using BackgroundJobs.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace BackgroundJobs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundJobsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BackgroundJobsOptions>(configuration.GetSection(BackgroundJobsOptions.SectionName));

        services.AddDbContext<BackgroundJobsDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", BackgroundJobsConstants.SchemaName))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<BackgroundJobsUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<BackgroundJobsUnitOfWork>());
        services.AddScoped<IBackgroundJobExecutionService, BackgroundJobExecutionService>();
        services.AddScoped<IBackgroundJobRunner, BackgroundJobRunner>();
        services.AddScoped<ILogCleanupService, LogCleanupService>();
        services.AddScoped<EmailRetryJob>();
        services.AddScoped<TemporaryFileCleanupJob>();
        services.AddScoped<LogCleanupJob>();

        var backgroundJobsOptions = configuration
            .GetSection(BackgroundJobsOptions.SectionName)
            .Get<BackgroundJobsOptions>() ?? new BackgroundJobsOptions();

        if (!backgroundJobsOptions.Enabled)
        {
            return services;
        }

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("DefaultConnection is required when BackgroundJobs.Enabled is true.");
        }

        services.AddHangfire(config =>
            config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(
                    options => options.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire"
                    }));

        services.AddHangfireServer();

        return services;
    }

    public static WebApplication UseBackgroundJobsDashboard(
        this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;
        if (!options.Enabled || !options.Dashboard.Enabled)
        {
            return app;
        }

        var dashboardPath = string.IsNullOrWhiteSpace(options.Dashboard.Path)
            ? "/hangfire"
            : options.Dashboard.Path;

        app.UseHangfireDashboard(
            dashboardPath,
            new global::Hangfire.DashboardOptions
            {
                Authorization = [new HangfireDashboardAuthorizationFilter()],
                IgnoreAntiforgeryToken = true
            });

        return app;
    }

    public static WebApplication RegisterBackgroundJobsRecurringJobs(this WebApplication app)
    {
        RegisterBackgroundJobsRecurringJobs((IHost)app);
        return app;
    }

    public static void RegisterBackgroundJobsRecurringJobs(this IHost host)
    {
        var options = host.Services.GetRequiredService<IOptions<BackgroundJobsOptions>>().Value;
        if (!options.Enabled)
        {
            return;
        }

        using var scope = host.Services.CreateScope();
        var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        recurringJobs.AddOrUpdate<EmailRetryJob>(
            RecurringJobIds.EmailRetry,
            job => job.ExecuteAsync(CancellationToken.None),
            options.Schedules.EmailRetryCron);

        recurringJobs.AddOrUpdate<TemporaryFileCleanupJob>(
            RecurringJobIds.TemporaryFileCleanup,
            job => job.ExecuteAsync(CancellationToken.None),
            options.Schedules.TemporaryFileCleanupCron);

        if (options.LogCleanup.Enabled)
        {
            recurringJobs.AddOrUpdate<LogCleanupJob>(
                RecurringJobIds.LogCleanup,
                job => job.ExecuteAsync(CancellationToken.None),
                options.Schedules.LogCleanupCron);
        }
        else
        {
            recurringJobs.RemoveIfExists(RecurringJobIds.LogCleanup);
        }
    }
}
