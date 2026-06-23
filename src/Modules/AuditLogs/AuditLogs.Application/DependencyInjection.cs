using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuditLogs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuditLogsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
