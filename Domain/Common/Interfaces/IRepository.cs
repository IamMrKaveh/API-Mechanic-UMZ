namespace Domain.Common.Interfaces;

/// <summary>
/// Generic repository contract for the three basic CRUD operations
/// shared by most repositories: GetByIdAsync / AddAsync / Update.
/// Specific repositories extend this interface and only declare
/// their dedicated query methods.
/// </summary>
public interface IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default);

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    void Update(TEntity entity);
}
