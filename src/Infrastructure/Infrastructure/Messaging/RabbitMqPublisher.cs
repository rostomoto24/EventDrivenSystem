using System.Text;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using RabbitMQ.Client;
using ReliableEvents.Sample.Application.Abstractions;

namespace ReliableEvents.Sample.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqPublisher> logger) : IEventPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
    {
        using var activity = MessagingTelemetry.ActivitySource.StartActivity(
            "rabbitmq publish",
            ActivityKind.Producer);

        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", _options.ExchangeName);
        activity?.SetTag("messaging.rabbitmq.routing_key", routingKey);
        activity?.SetTag("messaging.operation.name", "publish");

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(_options.QueueName, _options.ExchangeName, routingKey);

        var body = Encoding.UTF8.GetBytes(payload);
        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;
        MessagingTelemetry.InjectTraceContext(
            properties,
            new PropagationContext(activity?.Context ?? Activity.Current?.Context ?? default, Baggage.Current));

        channel.BasicPublish(_options.ExchangeName, routingKey, properties, body);
        logger.LogInformation(
            "Published message to RabbitMQ exchange {ExchangeName} with routing key {RoutingKey}.",
            _options.ExchangeName,
            routingKey);

        return Task.CompletedTask;
    }
}
