namespace ReliableEvents.Sample.Application.Abstractions;

public interface IIdempotencyStore
{
    Task<bool> HasBeenProcessedAsync(string key, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default);
}
