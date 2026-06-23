using Microsoft.Extensions.DependencyInjection;
using Users.Application;

namespace Users.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersApi(this IServiceCollection services)
    {
        services.AddUsersApplication();
        return services;
    }

    public static IMvcBuilder AddUsersPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
