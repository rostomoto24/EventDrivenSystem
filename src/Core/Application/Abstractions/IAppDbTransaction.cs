namespace ReliableEvents.Sample.Application.Abstractions;

public interface IAppDbTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
