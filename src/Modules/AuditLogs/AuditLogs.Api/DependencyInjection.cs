using AuditLogs.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogsApi(this IServiceCollection services)
    {
        services.AddAuditLogsApplication();
        return services;
    }

    public static IMvcBuilder AddAuditLogsPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
