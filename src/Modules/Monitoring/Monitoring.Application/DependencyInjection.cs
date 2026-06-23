using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Monitoring.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMonitoringApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
