using Domain.Category.Interfaces;
using Domain.Category.ValueObjects;

namespace Infrastructure.Category.Repositories;

public sealed class CategoryRepository(DBContext context)
    : Persistence.Repositories.RepositoryBase<Domain.Category.Aggregates.Category, CategoryId>(context), ICategoryRepository
{
    public Task<bool> ExistsByNameAsync(CategoryName name, CategoryId? excludeId = null, CancellationToken ct = default)
        => Context.Categories
            .AnyAsync(c => c.Name.Value == name.Value && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> ExistsBySlugAsync(CategorySlug slug, CategoryId? excludeId = null, CancellationToken ct = default)
        => Context.Categories
            .AnyAsync(c => c.Slug.Value == slug.Value && (excludeId == null || c.Id != excludeId), ct);

    public Task<bool> HasBrandAsync(CategoryId id, CancellationToken ct = default)
        => Context.Categories
            .Where(c => c.Id == id)
            .AnyAsync(c => c.Brands.Any(), ct);

    public void Update(Domain.Category.Aggregates.Category category, byte[]? rowVersion = null)
    {
        base.Update(category);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(category, rowVersion);
    }

    public void SetOriginalRowVersion(Domain.Category.Aggregates.Category entity, byte[] rowVersion)
        => Context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
}
