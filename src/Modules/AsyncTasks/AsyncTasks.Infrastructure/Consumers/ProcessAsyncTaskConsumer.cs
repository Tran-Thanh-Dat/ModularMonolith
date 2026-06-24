using AsyncTasks.Application.Abstractions;
using AsyncTasks.Application.Contracts;
using AsyncTasks.Domain.Constants;
using AsyncTasks.Infrastructure.Options;
using AsyncTasks.Infrastructure.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AsyncTasks.Infrastructure.Consumers;

public sealed class ProcessAsyncTaskConsumer : IConsumer<ProcessAsyncTaskMessage>
{
    private readonly IAsyncTaskConsumerService _consumerService;
    private readonly MessageQueueOptions _queueOptions;
    private readonly ILogger<ProcessAsyncTaskConsumer> _logger;

    public ProcessAsyncTaskConsumer(
        IAsyncTaskConsumerService consumerService,
        IOptions<MessageQueueOptions> queueOptions,
        ILogger<ProcessAsyncTaskConsumer> logger)
    {
        _consumerService = consumerService;
        _queueOptions = queueOptions.Value;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProcessAsyncTaskMessage> context)
    {
        try
        {
            await _consumerService.ProcessMessageAsync(
                context.Message,
                nameof(ProcessAsyncTaskConsumer),
                context.CancellationToken);
        }
        catch (Exception exception)
        {
            var retryAttempt = context.GetRetryAttempt();
            var maxRetries = _queueOptions.Retry.RetryCount;

            if (retryAttempt >= maxRetries)
            {
                _logger.LogError(
                    exception,
                    "Async task {TaskNo} failed after {RetryAttempt} retry attempt(s); marking FAILED",
                    context.Message.TaskNo,
                    retryAttempt);

                await _consumerService.HandleFaultAsync(
                    context.Message,
                    TruncateError(exception.Message),
                    context.CancellationToken);
                return;
            }

            _logger.LogWarning(
                exception,
                "Async task processing failed for {TaskNo} (retry attempt {RetryAttempt}/{MaxRetries}); MassTransit will retry",
                context.Message.TaskNo,
                retryAttempt,
                maxRetries);
            throw;
        }
    }

    private static string TruncateError(string message) =>
        message.Length <= AsyncTasksConstants.MaxErrorMessageLength
            ? message
            : message[..AsyncTasksConstants.MaxErrorMessageLength];
}

public sealed class ProcessAsyncTaskFaultConsumer : IConsumer<Fault<ProcessAsyncTaskMessage>>
{
    private readonly IAsyncTaskConsumerService _consumerService;
    private readonly ILogger<ProcessAsyncTaskFaultConsumer> _logger;

    public ProcessAsyncTaskFaultConsumer(
        IAsyncTaskConsumerService consumerService,
        ILogger<ProcessAsyncTaskFaultConsumer> logger)
    {
        _consumerService = consumerService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<ProcessAsyncTaskMessage>> context)
    {
        var message = context.Message.Message;
        if (message is null)
        {
            return;
        }

        var errors = string.Join("; ", context.Message.Exceptions.Select(e => e.Message));
        if (errors.Length > AsyncTasksConstants.MaxErrorMessageLength)
        {
            errors = errors[..AsyncTasksConstants.MaxErrorMessageLength];
        }

        _logger.LogError(
            "Async task fault consumer marking task {TaskNo} as failed: {Errors}",
            message.TaskNo,
            errors);

        await _consumerService.HandleFaultAsync(
            message,
            errors,
            context.CancellationToken);
    }
}
