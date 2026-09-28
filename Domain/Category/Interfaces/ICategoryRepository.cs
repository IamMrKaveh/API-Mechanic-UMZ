using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;

namespace Domain.Category.Interfaces;

public interface ICategoryRepository : IRepository<Aggregates.Category, CategoryId>
{
    Task<bool> ExistsByNameAsync(CategoryName name, CategoryId? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(CategorySlug slug, CategoryId? excludeId = null, CancellationToken ct = default);

    Task<bool> HasBrandAsync(CategoryId id, CancellationToken ct = default);

    void Update(Aggregates.Category category, byte[]? rowVersion = null);

    void SetOriginalRowVersion(Aggregates.Category entity, byte[] rowVersion);
}
