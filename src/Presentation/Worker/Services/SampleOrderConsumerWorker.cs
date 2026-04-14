using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Application.Orders;
using ReliableEvents.Sample.Infrastructure.Messaging;

namespace ReliableEvents.Sample.Worker.Services;

public sealed class SampleOrderConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<SampleOrderConsumerWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitOptions = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = rabbitOptions.HostName,
            Port = rabbitOptions.Port,
            UserName = rabbitOptions.UserName,
            Password = rabbitOptions.Password
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.ExchangeDeclare(rabbitOptions.ExchangeName, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(rabbitOptions.QueueName, rabbitOptions.ExchangeName, rabbitOptions.RoutingKey);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            using var scope = scopeFactory.CreateScope();
            var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var orderEvent = JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(payload);

            if (orderEvent is null)
            {
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            var key = $"orders:processed:{orderEvent.OrderId}";
            if (await idempotencyStore.HasBeenProcessedAsync(key, stoppingToken))
            {
                logger.LogInformation("Skipping duplicate event for order {OrderId}", orderEvent.OrderId);
                channel.BasicAck(ea.DeliveryTag, false);
                return;
            }

            logger.LogInformation("Processing order {OrderId} for {CustomerEmail}", orderEvent.OrderId, orderEvent.CustomerEmail);
            await idempotencyStore.MarkAsProcessedAsync(key, TimeSpan.FromHours(24), stoppingToken);
            channel.BasicAck(ea.DeliveryTag, false);
        };

        channel.BasicConsume(rabbitOptions.QueueName, autoAck: false, consumer);
        return Task.CompletedTask;
    }
}
