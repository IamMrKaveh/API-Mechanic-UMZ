namespace Infrastructure.Persistence.Repositories;

public class CategoryGroupRepository : ICategoryGroupRepository
{
    private readonly LedkaContext _context;

    public CategoryGroupRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Category.CategoryGroup> Groups, int Total)> GetPagedAsync(int? categoryId, string? search, int page, int pageSize)
    {
        var query = _context.Set<Domain.Category.CategoryGroup>().AsNoTracking();

        if (categoryId.HasValue)
        {
            query = query.Where(cg => cg.CategoryId == categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(cg => cg.Name.ToLower().Contains(search.ToLower()));
        }

        var total = await query.CountAsync();
        var groups = await query
            .OrderBy(cg => cg.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(cg => cg.Products)
            .ToListAsync();

        return (groups, total);
    }

    public Task<Domain.Category.CategoryGroup?> GetByIdAsync(int id)
    {
        return _context.Set<Domain.Category.CategoryGroup>()
            .AsNoTracking()
            .Include(cg => cg.Products)
            .FirstOrDefaultAsync(cg => cg.Id == id);
    }

    public Task<Domain.Category.CategoryGroup?> GetByIdWithProductsAsync(int id)
    {
        return _context.Set<Domain.Category.CategoryGroup>().Include(g => g.Products).FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddAsync(Domain.Category.CategoryGroup group)
    {
        await _context.Set<Domain.Category.CategoryGroup>().AddAsync(group);
    }

    public Task<bool> ExistsAsync(string name, int categoryId, int? excludeId = null)
    {
        var query = _context.Set<Domain.Category.CategoryGroup>()
            .Where(cg => cg.Name == name && cg.CategoryId == categoryId);

        if (excludeId.HasValue)
        {
            query = query.Where(cg => cg.Id != excludeId.Value);
        }

        return query.AnyAsync();
    }

    public void Delete(Domain.Category.CategoryGroup group)
    {
        _context.Set<Domain.Category.CategoryGroup>().Remove(group);
    }

    public void Update(Domain.Category.CategoryGroup group)
    {
        _context.Set<Domain.Category.CategoryGroup>().Update(group);
    }

    public void SetOriginalRowVersion(Domain.Category.CategoryGroup group, byte[] rowVersion)
    {
        _context.Entry(group).Property("RowVersion").OriginalValue = rowVersion;
    }
}