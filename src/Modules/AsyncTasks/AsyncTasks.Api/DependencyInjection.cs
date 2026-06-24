using AsyncTasks.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncTasks.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAsyncTasksApi(this IServiceCollection services)
    {
        services.AddAsyncTasksApplication();
        return services;
    }

    public static IMvcBuilder AddAsyncTasksPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
