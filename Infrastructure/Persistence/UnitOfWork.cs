using Domain.Common.Interfaces;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Persistence;

public sealed class UnitOfWork(DBContext context) : IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return await context.SaveChangesAsync(ct);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null)
            return _currentTransaction;

        _currentTransaction = await context.Database.BeginTransactionAsync(ct);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
            throw new InvalidOperationException("No active transaction.");

        await _currentTransaction.CommitAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null)
            return;

        await _currentTransaction.RollbackAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task ExecuteStrategyAsync(Func<Task> action, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(action);
    }

    public async Task<T> ExecuteStrategyAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(action);
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        context.Dispose();
    }
}