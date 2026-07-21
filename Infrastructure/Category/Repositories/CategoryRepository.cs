using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Infrastructure.Category.Repositories;

public sealed class CategoryRepository(DBContext context) : ICategoryRepository
{
    public Task<Domain.Category.Aggregates.Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default)
        => context.Categories
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNameAsync(CategoryName name, CategoryId? excludeId = null, CancellationToken ct = default)
        => context.Categories
            .AnyAsync(c => c.Name.Value == name.Value && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> ExistsBySlugAsync(CategorySlug slug, CategoryId? excludeId = null, CancellationToken ct = default)
        => context.Categories
            .AnyAsync(c => c.Slug.Value == slug.Value && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasBrandAsync(CategoryId id, CancellationToken ct = default)
        => context.Categories
            .Where(c => c.Id == id)
            .AnyAsync(c => c.Brands.Any(), ct);

    public async Task AddAsync(Domain.Category.Aggregates.Category category, CancellationToken ct = default)
        => await context.Categories
            .AddAsync(category, ct);

    public void Update(Domain.Category.Aggregates.Category category, byte[]? rowVersion = null)
    {
        context.Categories.Update(category);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(category, rowVersion);
    }

    public void SetOriginalRowVersion(Domain.Category.Aggregates.Category entity, byte[] rowVersion)
        => context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
}
