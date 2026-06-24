using AsyncTasks.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AsyncTasks.Infrastructure.Health;

public sealed class RabbitMqHealthCheck : IHealthCheck
{
    private readonly MessageQueueOptions _options;

    public RabbitMqHealthCheck(IOptions<MessageQueueOptions> options) => _options = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return HealthCheckResult.Healthy("Message queue is disabled.");
        }

        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _options.RabbitMQ.Host,
                Port = _options.RabbitMQ.Port,
                VirtualHost = _options.RabbitMQ.VirtualHost,
                UserName = _options.RabbitMQ.Username,
                Password = _options.RabbitMQ.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5)
            };

            await using var connection = await factory.CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ is reachable.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unavailable.", exception);
        }
    }
}

public static class AsyncTasksHealthCheckExtensions
{
    public static bool TryAddRabbitMqHealthCheck(
        IHealthChecksBuilder builder,
        IConfiguration configuration,
        IEnumerable<string> tags,
        TimeSpan timeout)
    {
        var options = configuration.GetSection(MessageQueueOptionsSection.SectionName).Get<MessageQueueOptions>()
            ?? new MessageQueueOptions();

        if (!options.Enabled)
        {
            return false;
        }

        builder.AddCheck<RabbitMqHealthCheck>(
            "rabbitmq",
            tags: tags,
            timeout: timeout);

        return true;
    }
}
