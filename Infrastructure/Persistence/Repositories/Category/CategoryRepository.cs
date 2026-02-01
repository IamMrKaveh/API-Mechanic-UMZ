namespace Infrastructure.Persistence.Repositories.Category;

public class CategoryRepository : ICategoryRepository
{
    private readonly LedkaContext _context;

    public CategoryRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Category.Category> Categories, int TotalItems)> GetCategoriesAsync(string? search, int page, int pageSize)
    {
        var query = _context.Categories
            .Include(c => c.CategoryGroups)
            .ThenInclude(cg => cg.Products)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = PersianTextHelper.Normalize(search);
            query = query.Where(c => EF.Functions.ILike(c.Name, $"%{term}%"));
        }

        var totalItems = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalItems);
    }

    public async Task<IEnumerable<Domain.Category.Category>> GetAllCategoriesWithGroupsAsync()
    {
        return await _context.Categories
            .Include(c => c.CategoryGroups)
            .Where(c => c.IsActive)
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task<Domain.Category.Category?> GetCategoryWithGroupsByIdAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.CategoryGroups)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<(IEnumerable<Domain.Product.Product> Products, int TotalCount)> GetProductsByCategoryIdAsync(int categoryId, int page, int pageSize)
    {
        var query = _context.Products
            .Include(p => p.CategoryGroup)
            .Include(p => p.Variants)
            .ThenInclude(v => v.Images)
            .Where(p => p.CategoryGroup.CategoryId == categoryId && p.IsActive);

        var totalCount = await query.CountAsync();
        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (products, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.Categories.AsQueryable();
        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }
        return await query.AnyAsync(c => c.Name == name);
    }

    public async Task AddAsync(Domain.Category.Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public void Update(Domain.Category.Category category)
    {
        _context.Categories.Update(category);
    }

    public async Task<Domain.Category.Category?> GetCategoryWithProductsAsync(int id)
    {
        return await _context.Categories
            .Include(c => c.CategoryGroups)
            .ThenInclude(cg => cg.Products)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public void Delete(Domain.Category.Category category)
    {
        _context.Categories.Remove(category);
    }

    public void SetOriginalRowVersion(Domain.Category.Category category, byte[] rowVersion)
    {
        _context.Entry(category).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }
}