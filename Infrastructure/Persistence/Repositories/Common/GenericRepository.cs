using Infrastructure.Persistence.Interface.Common;

namespace Infrastructure.Persistence.Repositories.Common;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly LedkaContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(LedkaContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public void SetOriginalRowVersion(T entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
    }
}