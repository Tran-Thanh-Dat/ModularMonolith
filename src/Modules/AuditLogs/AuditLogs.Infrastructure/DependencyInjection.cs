using AuditLogs.Application.Abstractions;
using AuditLogs.Infrastructure.Persistence;
using AuditLogs.Infrastructure.Persistence.Interceptors;
using AuditLogs.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AuditLogsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "audit")));

        services.AddScoped<IAuditChangeBuffer, AuditChangeBuffer>();
        services.AddScoped<IActivityLogBuffer, ActivityLogBuffer>();
        services.AddScoped<AuditChangeTrackingInterceptor>();
        services.AddScoped<ISaveChangesInterceptor>(provider =>
            provider.GetRequiredService<AuditChangeTrackingInterceptor>());

        services.AddScoped<IRequestContextService, RequestContextService>();
        services.AddScoped<AuditLogService>();
        services.AddScoped<IAuditLogService>(provider => provider.GetRequiredService<AuditLogService>());
        services.AddScoped<IAuditLogReadService, AuditLogReadService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddScoped<IActivityLogReadService, ActivityLogReadService>();
        services.AddScoped<IPostCommitHook, ActivityLogPostCommitHook>();
        services.AddScoped<IAuthorizationMiddlewareResultHandler, AuthorizationFailureActivityLoggingHandler>();

        return services;
    }
}
