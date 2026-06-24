using AsyncTasks.Application.Abstractions;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Infrastructure.Consumers;
using AsyncTasks.Infrastructure.Options;
using AsyncTasks.Infrastructure.Persistence;
using AsyncTasks.Infrastructure.Processors;
using AsyncTasks.Infrastructure.Queue;
using AsyncTasks.Infrastructure.Services;
using BuildingBlocks.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;

namespace AsyncTasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddAsyncTasksInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MessageQueueOptions>(configuration.GetSection(MessageQueueOptionsSection.SectionName));

        services.AddDbContext<AsyncTasksDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", AsyncTasksConstants.SchemaName))
                .AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>()));

        services.AddScoped<AsyncTasksUnitOfWork>();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AsyncTasksUnitOfWork>());

        services.AddScoped<IAsyncTaskPublishBuffer, AsyncTaskPublishBuffer>();
        services.AddScoped<IAsyncTaskService, AsyncTaskService>();
        services.AddScoped<IAsyncTaskConsumerService, AsyncTaskConsumerService>();
        services.AddScoped<IPostCommitHook, AsyncTaskPublishPostCommitHook>();

        services.AddScoped<IAsyncTaskProcessor, EmailDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessor, FileProcessingDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessor, FailDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessor, LongRunningDemoAsyncTaskProcessor>();
        services.AddScoped<IAsyncTaskProcessorRegistry, AsyncTaskProcessorRegistry>();

        var queueOptions = configuration.GetSection(MessageQueueOptionsSection.SectionName).Get<MessageQueueOptions>()
            ?? new MessageQueueOptions();

        if (queueOptions.Enabled)
        {
            services.AddScoped<IAsyncTaskQueuePublisher, MassTransitAsyncTaskQueuePublisher>();

            services.AddMassTransit(busConfigurator =>
            {
                busConfigurator.AddConsumer<ProcessAsyncTaskConsumer>();
                busConfigurator.AddConsumer<ProcessAsyncTaskFaultConsumer>();

                busConfigurator.UsingRabbitMq((context, cfg) =>
                {
                    var options = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<MessageQueueOptions>>().Value;
                    var rabbit = options.RabbitMQ;

                    cfg.Host(rabbit.Host, (ushort)rabbit.Port, rabbit.VirtualHost, host =>
                    {
                        host.Username(rabbit.Username);
                        host.Password(rabbit.Password);
                        if (rabbit.UseSsl)
                        {
                            host.UseSsl();
                        }
                    });

                    // Explicit endpoints only — do not call ConfigureEndpoints (avoids duplicate consumer bindings).
                    cfg.ReceiveEndpoint(AsyncTasksConstants.ProcessQueueName, endpoint =>
                    {
                        endpoint.UseMessageRetry(retry =>
                            retry.Interval(options.Retry.RetryCount, TimeSpan.FromSeconds(options.Retry.IntervalSeconds)));
                        endpoint.ConfigureConsumer<ProcessAsyncTaskConsumer>(context);
                    });

                    cfg.ReceiveEndpoint(AsyncTasksConstants.FaultQueueName, endpoint =>
                    {
                        endpoint.ConfigureConsumer<ProcessAsyncTaskFaultConsumer>(context);
                    });
                });
            });
        }
        else
        {
            services.AddScoped<IAsyncTaskQueuePublisher, NoOpAsyncTaskQueuePublisher>();
        }

        return services;
    }
}
