using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Infrastructure.Messaging;
using ReliableEvents.Sample.Persistence;

namespace ReliableEvents.Sample.Worker.Services;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var routingKey = rabbitMqOptions.Value.RoutingKey;

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

            var pendingMessages = await dbContext.OutboxMessages
                .Where(x => x.PublishedAtUtc == null)
                .OrderBy(x => x.OccurredAtUtc)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var outboxMessage in pendingMessages)
            {
                await publisher.PublishAsync(routingKey, outboxMessage.Payload, stoppingToken);
                outboxMessage.MarkPublished(DateTime.UtcNow);
                logger.LogInformation("Published outbox message {OutboxMessageId}", outboxMessage.Id);
            }

            if (pendingMessages.Count > 0)
            {
                await dbContext.SaveChangesAsync(stoppingToken);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
