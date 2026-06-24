using Microsoft.Extensions.DependencyInjection;
using MasterData.Application;

namespace MasterData.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMasterDataApi(this IServiceCollection services)
    {
        services.AddMasterDataApplication();
        return services;
    }

    public static IMvcBuilder AddMasterDataPresentation(this IMvcBuilder mvcBuilder) =>
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
}
