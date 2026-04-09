using Application.Common.Exceptions;
using Domain.Common.Interfaces;
using Infrastructure.Persistence.Context;
using Infrastructure.Persistence.Outbox;

namespace Infrastructure.Common.Services;

public class UnitOfWork(DBContext context, IDbContextTransaction? currentTransaction) : IUnitOfWork
{
    private readonly DBContext _context = context;
    private IDbContextTransaction? _currentTransaction = currentTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var aggregatesToClear = PrepareOutboxMessages();
        try
        {
            var result = await _context.SaveChangesAsync(ct);
            foreach (var aggregate in aggregatesToClear)
                aggregate.ClearDomainEvents();
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("این رکورد توسط کاربر دیگری تغییر یافته است.", ex);
        }
    }

    private IReadOnlyList<AggregateRoot> PrepareOutboxMessages()
    {
        var domainEntities = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        var outboxMessages = domainEvents.Select(de => OutboxMessage.Create(de)).ToList();

        _context.OutboxMessages.AddRange(outboxMessages);

        return domainEntities.Select(e => e.Entity).ToList();
    }

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction != null) return _currentTransaction;
        _currentTransaction = await _context.Database.BeginTransactionAsync(ct);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null) throw new InvalidOperationException("No transaction started.");
        try
        {
            await SaveChangesAsync(ct);
            await _currentTransaction.CommitAsync(ct);
        }
        catch
        {
            await RollbackTransactionAsync(ct);
            throw;
        }
        finally { await DisposeTransactionAsync(); }
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction == null) return;
        try { await _currentTransaction.RollbackAsync(ct); }
        finally { await DisposeTransactionAsync(); }
    }

    public async Task<T> ExecuteStrategyAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(operation);
    }

    public async Task ExecuteStrategyAsync(
        Func<Task> operation,
        CancellationToken ct = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(operation);
    }

    private async Task DisposeTransactionAsync()
    {
        if (_currentTransaction != null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
    }
}