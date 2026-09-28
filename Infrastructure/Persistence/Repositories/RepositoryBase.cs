using Domain.Common.Interfaces;
using SharedKernel.Abstractions;

namespace Infrastructure.Persistence.Repositories;

/// <summary>
/// Single generic implementation of the three basic CRUD operations
/// (GetByIdAsync / AddAsync / Update) shared by most repositories.
/// Specific repositories inherit from this base and only implement
/// their dedicated query methods, overriding a base method only when
/// they need Includes or custom concurrency handling.
/// </summary>
public abstract class RepositoryBase<TEntity, TId>(DBContext context) : IRepository<TEntity, TId>
    where TEntity : Entity<TId>
    where TId : notnull
{
    protected DBContext Context => context;

    protected DbSet<TEntity> Set => context.Set<TEntity>();

    public virtual Task<TEntity?> GetByIdAsync(TId id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(e => e.Id.Equals(id), ct);

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Set.AddAsync(entity, ct);

    public virtual void Update(TEntity entity)
        => Set.Update(entity);
}
