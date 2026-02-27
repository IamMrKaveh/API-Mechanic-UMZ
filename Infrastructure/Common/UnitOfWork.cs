using Domain.Common;

namespace Infrastructure.Common;

public class UnitOfWork : IUnitOfWork
{
    private readonly Persistence.Context.DBContext _context;
    private readonly ILogger<UnitOfWork> _logger;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(Persistence.Context.DBContext context, ILogger<UnitOfWork> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesToClear = PrepareOutboxMessages();
        try
        {
            var result = await _context.SaveChangesAsync(cancellationToken);
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

        var outboxMessages = domainEvents.Select(de => OutboxMessage.From(de)).ToList();

        _context.OutboxMessages.AddRange(outboxMessages);

        return domainEntities.Select(e => e.Entity).ToList();
    }

    public async Task<IDisposable> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null) return _currentTransaction;
        _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return _currentTransaction;
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) throw new InvalidOperationException("No transaction started.");
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally { await DisposeTransactionAsync(); }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction == null) return;
        try { await _currentTransaction.RollbackAsync(cancellationToken); }
        finally { await DisposeTransactionAsync(); }
    }

    public async Task<T> ExecuteStrategyAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(operation);
    }

    public async Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default)
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