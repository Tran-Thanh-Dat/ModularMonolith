using Microsoft.Extensions.DependencyInjection;
using Organizations.Application;

namespace Organizations.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationsApi(this IServiceCollection services)
    {
        services.AddOrganizationsApplication();
        return services;
    }

    public static IMvcBuilder AddOrganizationsPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
