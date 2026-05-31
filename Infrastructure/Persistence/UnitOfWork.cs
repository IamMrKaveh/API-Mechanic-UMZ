namespace Infrastructure.Persistence;

public sealed class UnitOfWork(DBContext context) : IUnitOfWork
{
    private bool disposed;

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        ThrowIfDisposed();
        await context.SaveChangesAsync(ct);
    }

    public async Task<T> ExecuteStrategyAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var efTransaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
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
        context.Dispose();
        disposed = true;
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;
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