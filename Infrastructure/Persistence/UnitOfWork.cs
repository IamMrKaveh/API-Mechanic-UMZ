namespace Infrastructure.Persistence;

public sealed class UnitOfWork(DBContext context) : IUnitOfWork
{
    private IDbContextTransaction? currentTransaction;
    private bool disposed;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return await context.SaveChangesAsync(ct);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (currentTransaction is not null)
            return currentTransaction;

        currentTransaction = await context.Database.BeginTransactionAsync(ct);
        return currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (currentTransaction is null)
            throw new InvalidOperationException("No active transaction.");

        try
        {
            await currentTransaction.CommitAsync(ct);
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
            await currentTransaction.RollbackAsync(ct);
        }
        finally
        {
            await currentTransaction.DisposeAsync();
            currentTransaction = null;
        }
    }

    public async Task<T> ExecuteStrategyAsync<T>(
        Func<IDbContextTransaction, CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(transaction, ct);
                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
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