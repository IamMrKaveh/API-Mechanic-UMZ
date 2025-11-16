namespace Application.Common.Interfaces.Persistence;

public interface ICategoryGroupRepository
{
    Task<(IEnumerable<Domain.Category.CategoryGroup> Groups, int Total)> GetPagedAsync(int? categoryId, string? search, int page, int pageSize);

    Task<Domain.Category.CategoryGroup?> GetByIdAsync(int id);

    Task<Domain.Category.CategoryGroup?> GetByIdWithProductsAsync(int id);

    Task AddAsync(Domain.Category.CategoryGroup group);

    void Update(Domain.Category.CategoryGroup group);

    Task<bool> ExistsAsync(string name, int categoryId, int? excludeId = null);

    void Delete(Domain.Category.CategoryGroup group);

    void SetOriginalRowVersion(Domain.Category.CategoryGroup group, byte[] rowVersion);
}