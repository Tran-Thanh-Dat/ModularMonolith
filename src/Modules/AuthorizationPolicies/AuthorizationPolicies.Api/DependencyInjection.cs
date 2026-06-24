using AuthorizationPolicies.Application;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationPolicies.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationPoliciesApi(this IServiceCollection services)
    {
        services.AddAuthorizationPoliciesApplication();
        return services;
    }

    public static IMvcBuilder AddAuthorizationPoliciesPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
