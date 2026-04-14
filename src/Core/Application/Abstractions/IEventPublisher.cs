namespace ReliableEvents.Sample.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, string payload, CancellationToken cancellationToken = default);
}
