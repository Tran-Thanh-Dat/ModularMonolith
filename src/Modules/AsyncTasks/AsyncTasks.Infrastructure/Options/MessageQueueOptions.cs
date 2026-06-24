namespace AsyncTasks.Infrastructure.Options;

public static class MessageQueueOptionsSection
{
    public const string SectionName = "MessageQueue";
}

public sealed class MessageQueueOptions
{
    public bool Enabled { get; set; }

    public string Provider { get; set; } = "RabbitMQ";

    public RabbitMqOptions RabbitMQ { get; set; } = new();

    public MessageQueueRetryOptions Retry { get; set; } = new();
}

public sealed class RabbitMqOptions
{
    public string Host { get; set; } = "localhost";

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public int Port { get; set; } = 5672;

    public bool UseSsl { get; set; }
}

public sealed class MessageQueueRetryOptions
{
    public int RetryCount { get; set; } = 3;

    public int IntervalSeconds { get; set; } = 5;
}
