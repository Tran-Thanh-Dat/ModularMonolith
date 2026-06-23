using System.Reflection;
using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.Logging;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBuildingBlocksApplication(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] mediatorAssemblies)
    {
        services.Configure<LoggingOptions>(configuration.GetSection(LoggingOptions.SectionName));

        services.AddOptions<SensitiveDataOptions>()
            .Configure<Microsoft.Extensions.Options.IOptions<LoggingOptions>>((options, loggingOptionsAccessor) =>
            {
                var sensitiveFields = loggingOptionsAccessor.Value.SensitiveFields;
                if (sensitiveFields is { Length: > 0 })
                {
                    options.SensitiveFields = sensitiveFields;
                }
            });

        services.AddSingleton<SensitiveDataMasker>();

        services.AddMediatR(configuration =>
        {
            foreach (var assembly in mediatorAssemblies)
            {
                configuration.RegisterServicesFromAssembly(assembly);
            }

            configuration.AddOpenBehavior(typeof(LoggingBehavior<,>));
            configuration.AddOpenBehavior(typeof(ValidationBehavior<,>));
            configuration.AddOpenBehavior(typeof(TransactionBehavior<,>));
            configuration.AddOpenBehavior(typeof(BusinessExceptionBehavior<,>));
        });

        foreach (var assembly in mediatorAssemblies)
        {
            services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        }

        return services;
    }
}
