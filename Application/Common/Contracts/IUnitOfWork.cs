namespace Application.Common.Contracts;

/// <summary>
/// Unit of Work - تعریف شده در Application
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task<T> ExecuteStrategyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default);
}