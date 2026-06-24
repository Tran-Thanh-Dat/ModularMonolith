using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AuthorizationPolicies.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAuthorizationPoliciesApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
