using Microsoft.EntityFrameworkCore.Storage;

namespace Domain.Common.Interfaces;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);

    Task<T> ExecuteStrategyAsync<T>(
        Func<IDbContextTransaction, CancellationToken, Task<T>> operation,
        CancellationToken ct = default);
}