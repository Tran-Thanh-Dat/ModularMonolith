using Microsoft.Extensions.DependencyInjection;
using Notifications.Application;

namespace Notifications.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsApi(this IServiceCollection services)
    {
        services.AddNotificationsApplication();
        return services;
    }

    public static IMvcBuilder AddNotificationsPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
