using Identity.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Identity.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityApi(this IServiceCollection services)
    {
        services.AddIdentityApplication();
        return services;
    }

    public static IMvcBuilder AddIdentityPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
