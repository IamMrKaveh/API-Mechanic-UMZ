using Application.Common.Interfaces;
using ITransaction = Application.Common.Interfaces.ITransaction;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork(DBContext context) : IUnitOfWork
{
    private EfCoreTransaction? currentTransaction;
    private bool disposed;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await context.SaveChangesAsync(ct);
    }

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (currentTransaction is not null)
            return currentTransaction;

        var efTransaction = await context.Database.BeginTransactionAsync(ct);
        currentTransaction = new EfCoreTransaction(efTransaction);
        return currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (currentTransaction is null)
            throw new InvalidOperationException("No active transaction.");

        try
        {
            await currentTransaction.Inner.CommitAsync(ct);
        }
        finally
        {
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (currentTransaction is null)
            return;

        try
        {
            await currentTransaction.Inner.RollbackAsync(ct);
        }
        finally
        {
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }
    }

    public async Task<T> ExecuteStrategyAsync<T>(
        Func<ITransaction, CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            var efTransaction = await context.Database.BeginTransactionAsync(ct);
            await using var transaction = new EfCoreTransaction(efTransaction);
            try
            {
                var result = await operation(transaction, ct);
                await efTransaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await efTransaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public void Dispose()
    {
        if (disposed) return;

        currentTransaction?.Dispose();
        context.Dispose();

        disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;

        if (currentTransaction is not null)
        {
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }

        await context.DisposeAsync();

        disposed = true;
        GC.SuppressFinalize(this);
    }

    private void ThrowIfDisposed()
    {
        if (!disposed) return;
        throw new ObjectDisposedException(nameof(UnitOfWork));
    }
}