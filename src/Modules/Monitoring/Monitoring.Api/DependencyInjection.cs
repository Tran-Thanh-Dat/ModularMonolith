using Microsoft.Extensions.DependencyInjection;
using Monitoring.Application;

namespace Monitoring.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMonitoringApi(this IServiceCollection services)
    {
        services.AddMonitoringApplication();
        return services;
    }

    public static IMvcBuilder AddMonitoringPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
