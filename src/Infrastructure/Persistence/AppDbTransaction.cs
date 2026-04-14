using Microsoft.EntityFrameworkCore.Storage;
using ReliableEvents.Sample.Application.Abstractions;

namespace ReliableEvents.Sample.Persistence;

public sealed class AppDbTransaction(IDbContextTransaction transaction) : IAppDbTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken = default)
        => transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync()
        => transaction.DisposeAsync();
}
