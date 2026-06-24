using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AsyncTasks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddAsyncTasksApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
