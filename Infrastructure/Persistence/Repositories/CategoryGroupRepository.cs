using Application.Common.Utilities;

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
        var query = _context.CategoryGroups
            .Include(g => g.Category)
            .Include(g => g.Products)
            .AsQueryable();

        if (categoryId.HasValue)
        {
            query = query.Where(g => g.CategoryId == categoryId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = PersianTextHelper.Normalize(search);
            query = query.Where(g =>
                EF.Functions.ILike(g.Name, $"%{term}%") ||
                EF.Functions.ILike(g.Category.Name, $"%{term}%"));
        }

        var total = await query.CountAsync();
        var groups = await query
            .OrderBy(g => g.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (groups, total);
    }

    public async Task<Domain.Category.CategoryGroup?> GetByIdAsync(int id)
    {
        return await _context.CategoryGroups
            .Include(g => g.Category)
            .Include(g => g.Products)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Domain.Category.CategoryGroup?> GetByIdWithProductsAsync(int id)
    {
        return await _context.CategoryGroups
            .Include(g => g.Products)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task AddAsync(Domain.Category.CategoryGroup group)
    {
        await _context.CategoryGroups.AddAsync(group);
    }

    public void Update(Domain.Category.CategoryGroup group)
    {
        _context.CategoryGroups.Update(group);
    }

    public async Task<bool> ExistsAsync(string name, int categoryId, int? excludeId = null)
    {
        var query = _context.CategoryGroups.Where(g => g.CategoryId == categoryId);
        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }
        return await query.AnyAsync(g => g.Name == name);
    }

    public void Delete(Domain.Category.CategoryGroup group)
    {
        _context.CategoryGroups.Remove(group);
    }

    public void SetOriginalRowVersion(Domain.Category.CategoryGroup group, byte[] rowVersion)
    {
        _context.Entry(group).Property(x => x.RowVersion).OriginalValue = rowVersion;
    }
}