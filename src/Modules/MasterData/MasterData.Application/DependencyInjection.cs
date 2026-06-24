using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MasterData.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMasterDataApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}
