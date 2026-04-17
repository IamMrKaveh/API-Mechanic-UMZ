using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Infrastructure.Category.Repositories;

public sealed class CategoryRepository(DBContext context) : ICategoryRepository
{
    public async Task<Domain.Category.Aggregates.Category?> GetByIdAsync(
        CategoryId id,
        CancellationToken ct = default)
        => await context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<Domain.Category.Aggregates.Category?> GetBySlugAsync(
        Slug slug,
        CancellationToken ct = default)
        => await context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug, ct);

    public async Task<IReadOnlyList<Domain.Category.Aggregates.Category>> GetAllActiveAsync(
        CancellationToken ct = default)
    {
        var results = await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);
        return results.AsReadOnly();
    }

    public async Task<bool> ExistsByNameAsync(
        CategoryName name,
        CategoryId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.Categories.Where(c => c.Name == name);
        if (excludeId is not null)
            query = query.Where(c => c.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExistsBySlugAsync(
        Slug slug,
        CategoryId? excludeId = null,
        CancellationToken ct = default)
    {
        var query = context.Categories.Where(c => c.Slug == slug);
        if (excludeId is not null)
            query = query.Where(c => c.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> HasBrandAsync(
        CategoryId id,
        CancellationToken ct = default)
        => await context.Brands.AnyAsync(b => b.CategoryId == id, ct);

    public async Task AddAsync(
        Domain.Category.Aggregates.Category category,
        CancellationToken ct = default)
        => await context.Categories.AddAsync(category, ct);

    public void Update(Domain.Category.Aggregates.Category category)
        => context.Categories.Update(category);

    public void SetOriginalRowVersion(
        Domain.Category.Aggregates.Category entity,
        byte[] rowVersion)
        => context.Entry(entity).OriginalValues["RowVersion"] = rowVersion;
}