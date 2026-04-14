using ReliableEvents.Sample.Domain;

namespace ReliableEvents.Sample.Application.Abstractions;

public interface IAppDbContext
{
    Task AddOrderAsync(Order order, CancellationToken cancellationToken = default);
    Task AddOutboxMessageAsync(OutboxMessage outboxMessage, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingOutboxMessagesAsync(int batchSize, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IAppDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
