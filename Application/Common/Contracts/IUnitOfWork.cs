namespace Application.Common.Contracts;

/// <summary>
/// Unit of Work - تعریف شده در Application
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(
        CancellationToken ct = default
        );

    Task<IDisposable> BeginTransactionAsync(
        CancellationToken ct = default
        );

    Task CommitTransactionAsync(
        CancellationToken ct = default
        );

    Task RollbackTransactionAsync(
        CancellationToken ct = default
        );

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task<T> ExecuteStrategyAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default
        );

    /// <summary>
    /// Execute an operation within a database execution strategy (for retry logic)
    /// </summary>
    Task ExecuteStrategyAsync(
        Func<Task> operation,
        CancellationToken ct = default
        );
}