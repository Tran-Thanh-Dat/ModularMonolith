using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Abstractions;
using Users.Infrastructure.Services;

namespace Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddUsersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = configuration;
        services.AddScoped<IUserManagementService, UserManagementService>();
        return services;
    }
}
