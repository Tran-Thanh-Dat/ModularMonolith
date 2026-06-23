using Microsoft.Extensions.DependencyInjection;
using Settings.Application;

namespace Settings.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddSettingsApi(this IServiceCollection services)
    {
        services.AddSettingsApplication();
        return services;
    }

    public static IMvcBuilder AddSettingsPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
