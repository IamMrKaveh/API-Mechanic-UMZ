namespace Infrastructure.Categories.Repositories;

/// <summary>
/// Repository فقط برای Aggregate Root (Category).
/// CategoryGroup ریپازیتوری مستقل ندارد.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly LedkaContext _context;

    public CategoryRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdWithGroupsAsync(
        int id, CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.CategoryGroups.Where(g => !g.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Category?> GetByIdWithGroupsAndProductsAsync(
        int id, CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.CategoryGroups.Where(g => !g.IsDeleted))
                .ThenInclude(g => g.Products.Where(p => !p.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Category>> GetByIdsAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted)
            .Include(c => c.CategoryGroups.Where(g => !g.IsDeleted))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Category>> GetAllActiveAsync(
        CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(
        string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var query = _context.Categories
            .Where(c => c.Name.Value.ToLower() == normalizedName && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(
        string slug, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var query = _context.Categories
            .Where(c => c.Slug != null && c.Slug.Value.ToLower() == normalizedSlug && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Category category, CancellationToken ct = default)
    {
        await _context.Categories.AddAsync(category, ct);
    }

    public void Update(Category category)
    {
        _context.Categories.Update(category);
    }

    public void SetOriginalRowVersion(Category entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(c => c.RowVersion).OriginalValue = rowVersion;
    }
}