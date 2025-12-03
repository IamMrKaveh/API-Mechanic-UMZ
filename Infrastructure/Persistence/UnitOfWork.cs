namespace Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly LedkaContext _context;

    public UnitOfWork(LedkaContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public Task<TResult> ExecuteStrategyAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }

    public Task ExecuteStrategyAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async ct => await operation(), cancellationToken);
    }
}