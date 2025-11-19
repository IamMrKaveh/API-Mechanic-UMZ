namespace Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly LedkaContext _context;

    public CategoryRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Category.Category> Categories, int TotalItems)> GetCategoriesAsync(string? search, int page, int pageSize)
    {
        var query = _context.Set<Domain.Category.Category>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name != null && c.Name.ToLower().Contains(search.ToLower()));
        }

        var totalItems = await query.CountAsync();
        var categories = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(c => c.CategoryGroups)
                .ThenInclude(cg => cg.Products)
                    .ThenInclude(p => p.Variants)
            .AsNoTracking()
            .ToListAsync();

        return (categories, totalItems);
    }

    public Task<Domain.Category.Category?> GetCategoryWithGroupsByIdAsync(int id)
    {
        return _context.Set<Domain.Category.Category>()
            .AsNoTracking()
            .Include(c => c.CategoryGroups)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(IEnumerable<Domain.Product.Product> Products, int TotalCount)> GetProductsByCategoryIdAsync(int categoryId, int page, int pageSize)
    {
        var query = _context.Set<Domain.Product.Product>().Where(p => p.CategoryGroup.CategoryId == categoryId);
        var totalCount = await query.CountAsync();
        var products = await query
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.Set<Domain.Category.Category>().Where(c => c.Name != null && c.Name.ToLower() == name.ToLower());
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return query.AnyAsync();
    }

    public async Task AddAsync(Domain.Category.Category category)
    {
        await _context.Set<Domain.Category.Category>().AddAsync(category);
    }

    public void Update(Domain.Category.Category category)
    {
        _context.Set<Domain.Category.Category>().Update(category);
    }

    public Task<Domain.Category.Category?> GetCategoryWithProductsAsync(int id)
    {
        return _context.Set<Domain.Category.Category>()
            .Include(c => c.CategoryGroups)
            .ThenInclude(cg => cg.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public void Delete(Domain.Category.Category category)
    {
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;
        _context.Set<Domain.Category.Category>().Update(category);
    }

    public void SetOriginalRowVersion(Domain.Category.Category category, byte[] rowVersion)
    {
        _context.Entry(category).Property("RowVersion").OriginalValue = rowVersion;
    }
}