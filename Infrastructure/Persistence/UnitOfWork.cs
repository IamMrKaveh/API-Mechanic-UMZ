using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork(
    DBContext context,
    ILogger<UnitOfWork> logger) : IUnitOfWork
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
        => await ExecuteTransactionAsync(operation, _ => true, ct);

    public async Task<T> ExecuteTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldCommit,
        CancellationToken ct = default)
    {
        ThrowIfDisposed();

        if (context.Database.CurrentTransaction is not null)
        {
            // A transaction is already running: the operation joins it through a
            // savepoint, so a failure inside it rolls back only its own writes.
            return await ExecuteInSavepointAsync(operation, ct);
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction efTransaction = await context.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await operation(ct);

                if (!shouldCommit(result))
                {
                    logger.LogWarning(
                        "Transaction rolled back because the operation reported a rejected result.");
                    await efTransaction.RollbackAsync(CancellationToken.None);
                    return result;
                }

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

    private async Task<T> ExecuteInSavepointAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken ct)
    {
        if (!context.Database.SupportsSavepoints())
        {
            logger.LogWarning(
                "The ambient transaction does not support savepoints; the operation joins it directly.");
            return await operation(ct);
        }

        var savepoint = $"sp_{Guid.NewGuid():N}";

        await context.Database.CreateSavepointAsync(savepoint, ct);

        try
        {
            return await operation(ct);
        }
        catch
        {
            logger.LogWarning(
                "Operation failed inside an ambient transaction; rolling back to savepoint {Savepoint}.",
                savepoint);

            await context.Database.RollbackToSavepointAsync(savepoint, ct);
            throw;
        }
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
