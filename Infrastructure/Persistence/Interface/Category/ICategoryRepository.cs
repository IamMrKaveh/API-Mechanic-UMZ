namespace Infrastructure.Persistence.Interface.Category;

public interface ICategoryRepository
{
    Task<(IEnumerable<Domain.Category.Category> Categories, int TotalItems)> GetCategoriesAsync(string? search, int page, int pageSize);

    Task<IEnumerable<Domain.Category.Category>> GetAllCategoriesWithGroupsAsync();

    Task<Domain.Category.Category?> GetCategoryWithGroupsByIdAsync(int id);

    Task<(IEnumerable<Product> Products, int TotalCount)> GetProductsByCategoryIdAsync(int categoryId, int page, int pageSize);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);

    Task AddAsync(Domain.Category.Category category);

    void Update(Domain.Category.Category category);

    Task<Domain.Category.Category?> GetCategoryWithProductsAsync(int id);

    void Delete(Domain.Category.Category category);

    void SetOriginalRowVersion(Domain.Category.Category category, byte[] rowVersion);
}