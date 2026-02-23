namespace Infrastructure.Category.Repositories;

/// <summary>
/// Repository فقط برای Aggregate Root (Category).
/// Brand ریپازیتوری مستقل ندارد.
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    private readonly Persistence.Context.DBContext _context;

    public CategoryRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Category.Category> Items, int TotalCount)> GetPagedAsync(
        string? search, bool? isActive, bool includeDeleted, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.Brands.Where(g => !g.IsDeleted))
                .ThenInclude(b => b.Products.Where(p => !p.IsDeleted))
            .AsQueryable();

        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);
        else
            query = query.IgnoreQueryFilters();

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();
            query = query.Where(c => c.Name.Value.ToLower().Contains(searchTerm));
        }

        var totalItems = await query.CountAsync(ct);

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ThenByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (categories, totalItems);
    }

    public async Task<Domain.Category.Category?> GetByIdWithGroupsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.Brands.Where(g => !g.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Domain.Category.Category?> GetByIdWithGroupsAndProductsAsync(int id, CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.Brands.Where(g => !g.IsDeleted))
                .ThenInclude(g => g.Products.Where(p => !p.IsDeleted))
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Domain.Category.Category>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => ids.Contains(c.Id) && !c.IsDeleted)
            .Include(c => c.Brands.Where(g => !g.IsDeleted))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Category.Category>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var query = _context.Categories
            .Where(c => c.Name.Value.ToLower() == normalizedName && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var query = _context.Categories
            .Where(c => c.Slug != null && c.Slug.Value.ToLower() == normalizedSlug && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.Category.Category category, CancellationToken ct = default)
    {
        await _context.Categories.AddAsync(category, ct);
    }

    public void Update(Domain.Category.Category category)
    {
        _context.Categories.Update(category);
    }

    public void SetOriginalRowVersion(Domain.Category.Category entity, byte[] rowVersion)
    {
        _context.Entry(entity).Property(c => c.RowVersion).OriginalValue = rowVersion;
    }
}