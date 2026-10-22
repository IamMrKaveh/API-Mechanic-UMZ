using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Common.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<T> ExecuteStrategyAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Runs the operation inside a transaction and commits it only when
    /// <paramref name="shouldCommit"/> accepts the result. A rejected result rolls
    /// the transaction back, so a failed command never leaves half of its writes
    /// behind.
    /// </summary>
    Task<T> ExecuteTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken ct = default);
}
