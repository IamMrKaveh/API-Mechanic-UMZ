using Domain.Category.Interfaces;

namespace Infrastructure.Category.Repositories;

public class CategoryRepository(DBContext context) : ICategoryRepository
{
    private readonly DBContext _context = context;

    public async Task<Domain.Category.Aggregates.Category?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Domain.Category.Aggregates.Category?> GetByIdWithGroupsAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.Brands)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Domain.Category.Aggregates.Category?> GetByIdWithGroupsAndProductsAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Categories
            .Include(c => c.Brands)
                .ThenInclude(b => b.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<IReadOnlyList<Domain.Category.Aggregates.Category>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return await _context.Categories
            .Where(c => idList.Contains(c.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Domain.Category.Aggregates.Category>> GetAllActiveAsync(
        CancellationToken ct = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive && !c.IsDeleted)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Categories
            .Where(c => c.Name == name && !c.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        Domain.Category.Aggregates.Category category,
        CancellationToken ct = default)
    {
        await _context.Categories.AddAsync(category, ct);
    }

    public void Update(Domain.Category.Aggregates.Category category)
    {
        _context.Categories.Update(category);
    }

    public void SetOriginalRowVersion(
        Domain.Category.Aggregates.Category entity,
        byte[] rowVersion)
    {
        _context.Entry(entity).Property(c => c.RowVersion).OriginalValue = rowVersion;
    }
}