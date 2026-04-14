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
    private static readonly TimeSpan ProcessedEventTtl = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rabbitOptions = options.Value;
        var factory = new ConnectionFactory
        {
            HostName = rabbitOptions.HostName,
            Port = rabbitOptions.Port,
            UserName = rabbitOptions.UserName,
            Password = rabbitOptions.Password
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.ExchangeDeclare(rabbitOptions.ExchangeName, ExchangeType.Direct, durable: true);
        channel.QueueDeclare(rabbitOptions.QueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(rabbitOptions.QueueName, rabbitOptions.ExchangeName, rabbitOptions.RoutingKey);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) => await HandleOrderCreatedAsync(channel, ea, stoppingToken);

        channel.BasicConsume(rabbitOptions.QueueName, autoAck: false, consumer);

        logger.LogInformation(
            "Sample order consumer started. Queue: {QueueName}, RoutingKey: {RoutingKey}",
            rabbitOptions.QueueName,
            rabbitOptions.RoutingKey);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Sample order consumer is stopping.");
        }
    }

    private async Task HandleOrderCreatedAsync(
        IModel channel,
        BasicDeliverEventArgs ea,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var idempotencyStore = scope.ServiceProvider.GetRequiredService<IIdempotencyStore>();

            var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
            var orderEvent = JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(payload);

            if (orderEvent is null)
            {
                logger.LogWarning("Received invalid OrderCreated payload. Acknowledging message.");
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            var idempotencyKey = BuildOrderCreatedIdempotencyKey(orderEvent.OrderId);
            if (await idempotencyStore.HasBeenProcessedAsync(idempotencyKey, cancellationToken))
            {
                logger.LogInformation(
                    "Skipping duplicate OrderCreated event for order {OrderId}. Idempotency key: {IdempotencyKey}",
                    orderEvent.OrderId,
                    idempotencyKey);
                channel.BasicAck(ea.DeliveryTag, multiple: false);
                return;
            }

            logger.LogInformation(
                "Handling OrderCreated event for order {OrderId} and customer {CustomerEmail}",
                orderEvent.OrderId,
                orderEvent.CustomerEmail);

            await idempotencyStore.MarkAsProcessedAsync(idempotencyKey, ProcessedEventTtl, cancellationToken);
            channel.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle OrderCreated event. Message will be requeued.");
            channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
        }
    }

    private static string BuildOrderCreatedIdempotencyKey(Guid orderId)
        => $"idempotency:consumer:order-created:{orderId:N}";
}
