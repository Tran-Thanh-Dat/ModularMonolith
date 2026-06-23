using AuditLogs.Application;
using BuildingBlocks.Application;
using BuildingBlocks.Infrastructure;
using BuildingBlocks.Web;
using BackgroundJobs.Application;
using ApiHost.Middlewares;
using Categories.Application;
using Files.Application;
using Identity.Application;
using Monitoring.Application;
using Notifications.Application;
using Settings.Application;
using Users.Application;

namespace ApiHost.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBuildingBlocksApplication(
            configuration,
            typeof(Program).Assembly,
            typeof(Identity.Application.DependencyInjection).Assembly,
            typeof(Users.Application.DependencyInjection).Assembly,
            typeof(AuditLogs.Application.DependencyInjection).Assembly,
            typeof(Categories.Application.DependencyInjection).Assembly,
            typeof(Settings.Application.DependencyInjection).Assembly,
            typeof(Files.Application.DependencyInjection).Assembly,
            typeof(Notifications.Application.DependencyInjection).Assembly,
            typeof(BackgroundJobs.Application.DependencyInjection).Assembly,
            typeof(Monitoring.Application.DependencyInjection).Assembly);

        services.AddIdentityApplication();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddBuildingBlocksInfrastructure(configuration, environment);

        return services;
    }

    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Modular Monolith API",
                Version = "v1",
                Description =
                    ".NET 8 modular monolith Web API. Authenticate via POST /api/v1/auth/login, " +
                    "then use the access token as Bearer authorization. See docs/README.md in the repository."
            });

            options.TagActionsBy(api =>
            {
                if (api.GroupName is not null)
                {
                    return [api.GroupName];
                }

                var controllerName = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var name)
                    ? name
                    : "Default";

                return [controllerName];
            });

            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme.",
                Name = "Authorization",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
            {
                {
                    new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                    {
                        Reference = new Microsoft.OpenApi.Models.OpenApiReference
                        {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
