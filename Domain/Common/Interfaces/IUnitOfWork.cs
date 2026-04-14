using Microsoft.EntityFrameworkCore.Storage;

namespace Domain.Common.Interfaces;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task<T> ExecuteStrategyAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default);

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task ExecuteStrategyAsync(
        Func<Task> operation,
        CancellationToken ct = default);
}