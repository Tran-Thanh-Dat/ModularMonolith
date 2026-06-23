using Files.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Files.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesApi(this IServiceCollection services)
    {
        services.AddFilesApplication();
        return services;
    }

    public static IMvcBuilder AddFilesPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
