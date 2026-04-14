namespace ReliableEvents.Sample.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "reliable-events";
    public string QueueName { get; set; } = "orders-created";
    public string RoutingKey { get; set; } = "orders.created";
}
