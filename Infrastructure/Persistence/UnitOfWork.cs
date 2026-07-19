namespace Infrastructure.Persistence;

public sealed class UnitOfWork(
    DBContext context,
    ILogger<UnitOfWork> logger,
    IHostEnvironment environment) : IUnitOfWork
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

        if (context.Database.CurrentTransaction is not null)
        {
            if (environment.IsDevelopment())
                throw new InvalidOperationException(
                    "ExecuteStrategyAsync was called while a transaction is already active. " +
                    "Wrap the entire transactional block (including BeginTransaction) inside the strategy.");

            logger.LogWarning(
                "ExecuteStrategyAsync invoked while a transaction is already active. " +
                "Operation is running without retry semantics inside the outer transaction.");

            return await operation(ct);
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction efTransaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);
                await efTransaction.CommitAsync(ct);
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Transaction rolled back due to exception.");
                await efTransaction.RollbackAsync(CancellationToken.None);
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

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(disposed, nameof(UnitOfWork));
}
