using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Files.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);
        return services;
    }
}
