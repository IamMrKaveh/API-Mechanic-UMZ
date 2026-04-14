using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Category.Repositories;

public sealed class CategoryRepository(DBContext context) : ICategoryRepository
{
    public async Task<Domain.Category.Aggregates.Category?> GetByIdAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        return await context.Categories
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct);
    }

    public async Task<Domain.Category.Aggregates.Category?> GetBySlugAsync(Slug slug, CancellationToken ct = default)
    {
        return await context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug.Value, ct);
    }

    public async Task<IReadOnlyList<Domain.Category.Aggregates.Category>> GetAllActiveAsync(CancellationToken ct = default)
    {
        var results = await context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<bool> ExistsBySlugAsync(Slug slug, CategoryId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Categories.Where(c => c.Slug == slug.Value);
        if (excludeId is not null)
            query = query.Where(c => c.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task<bool> HasBrandAsync(CategoryId categoryId, CancellationToken ct = default)
    {
        return await context.Brands.AnyAsync(b => b.CategoryId == categoryId, ct);
    }

    public async Task AddAsync(Domain.Category.Aggregates.Category category, CancellationToken ct = default)
    {
        await context.Categories.AddAsync(category, ct);
    }

    public void Update(Domain.Category.Aggregates.Category category)
    {
        context.Categories.Update(category);
    }
}