namespace Application.Common.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task<TResult> ExecuteStrategyAsync<TResult>(Func<Task<TResult>> operation, CancellationToken cancellationToken = default);
}