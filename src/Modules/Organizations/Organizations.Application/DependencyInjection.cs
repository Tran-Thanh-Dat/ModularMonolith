using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Organizations.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationsApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
