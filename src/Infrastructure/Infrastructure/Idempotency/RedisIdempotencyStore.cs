using ReliableEvents.Sample.Application.Abstractions;
using StackExchange.Redis;

namespace ReliableEvents.Sample.Infrastructure.Idempotency;

public sealed class RedisIdempotencyStore(IConnectionMultiplexer connectionMultiplexer) : IIdempotencyStore
{
    public async Task<bool> HasBeenProcessedAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = connectionMultiplexer.GetDatabase();
        return await db.KeyExistsAsync(key);
    }

    public async Task MarkAsProcessedAsync(string key, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var db = connectionMultiplexer.GetDatabase();
        await db.StringSetAsync(key, "1", ttl);
    }
}
