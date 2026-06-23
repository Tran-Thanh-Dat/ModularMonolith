using Microsoft.Extensions.DependencyInjection;
using Categories.Application;

namespace Categories.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCategoriesApi(this IServiceCollection services)
    {
        services.AddCategoriesApplication();
        return services;
    }

    public static IMvcBuilder AddCategoriesPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
