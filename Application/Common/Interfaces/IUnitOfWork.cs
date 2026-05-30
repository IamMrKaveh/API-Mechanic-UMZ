namespace Application.Common.Interfaces;

public interface IUnitOfWork : ITransaction
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);

    Task<T> ExecuteStrategyAsync<T>(
        Func<ITransaction, CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}