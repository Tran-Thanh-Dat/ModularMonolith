using BackgroundJobs.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BackgroundJobs.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddBackgroundJobsApi(this IServiceCollection services)
    {
        services.AddBackgroundJobsApplication();
        return services;
    }

    public static IMvcBuilder AddBackgroundJobsPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
