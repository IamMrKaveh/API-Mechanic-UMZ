namespace Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteStrategyAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}