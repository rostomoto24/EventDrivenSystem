using System.Text;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ReliableEvents.Sample.Application.Abstractions;

namespace ReliableEvents.Sample.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(IOptions<RabbitMqOptions> options) : IEventPublisher
{
    private readonly RabbitMqOptions _options = options.Value;

    public Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default)
    {
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

        channel.BasicPublish(_options.ExchangeName, routingKey, properties, body);
        return Task.CompletedTask;
    }
}
