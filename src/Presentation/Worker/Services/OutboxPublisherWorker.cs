using Microsoft.Extensions.Options;
using ReliableEvents.Sample.Application.Abstractions;
using ReliableEvents.Sample.Infrastructure.Messaging;

namespace ReliableEvents.Sample.Worker.Services;

public sealed class OutboxPublisherWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    ILogger<OutboxPublisherWorker> logger) : BackgroundService
{
    private const int BatchSize = 20;
    private const int MaxPublishAttempts = 3;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var routingKey = rabbitMqOptions.Value.RoutingKey;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                var pendingMessages = await dbContext.GetPendingOutboxMessagesAsync(BatchSize, stoppingToken);

                logger.LogInformation(
                    "Fetched outbox batch. Pending messages in batch: {PendingMessageCount}, RoutingKey: {RoutingKey}",
                    pendingMessages.Count,
                    routingKey);

                foreach (var outboxMessage in pendingMessages)
                {
                    using var _ = logger.BeginScope(new Dictionary<string, object>
                    {
                        ["OutboxMessageId"] = outboxMessage.Id,
                        ["RoutingKey"] = routingKey
                    });

                    var wasPublished = await TryPublishWithRetriesAsync(
                        publisher,
                        routingKey,
                        outboxMessage.Payload,
                        stoppingToken);

                    if (!wasPublished)
                    {
                        continue;
                    }

                    outboxMessage.MarkPublished(DateTime.UtcNow);
                    logger.LogInformation("Published outbox message successfully.");
                }

                if (pendingMessages.Any(x => x.PublishedAtUtc != null))
                {
                    await dbContext.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("Persisted published outbox messages to the database.");
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error in outbox publisher loop.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task<bool> TryPublishWithRetriesAsync(
        IEventPublisher publisher,
        string routingKey,
        string payload,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= MaxPublishAttempts; attempt++)
        {
            try
            {
                logger.LogInformation(
                    "Publishing outbox message attempt {Attempt} of {MaxAttempts}.",
                    attempt,
                    MaxPublishAttempts);

                await publisher.PublishAsync(routingKey, payload, cancellationToken);
                return true;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (attempt == MaxPublishAttempts)
                {
                    logger.LogError(
                        ex,
                        "Outbox publish failed after {MaxAttempts} attempts.",
                        MaxPublishAttempts);
                    return false;
                }

                logger.LogWarning(
                    ex,
                    "Outbox publish attempt {Attempt} failed. Retrying after {RetryDelayMs} ms.",
                    attempt,
                    RetryDelay.TotalMilliseconds);

                await Task.Delay(RetryDelay, cancellationToken);
            }
        }

        return false;
    }
}
