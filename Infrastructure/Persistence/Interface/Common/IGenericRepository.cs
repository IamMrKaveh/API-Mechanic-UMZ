namespace Infrastructure.Persistence.Interface.Common;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    void SetOriginalRowVersion(T entity, byte[] rowVersion);
}