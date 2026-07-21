using Domain.Category.ValueObjects;

namespace Domain.Category.Interfaces;

public interface ICategoryRepository
{
    Task<Aggregates.Category?> GetByIdAsync(CategoryId id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(CategoryName name, CategoryId? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(CategorySlug slug, CategoryId? excludeId = null, CancellationToken ct = default);

    Task<bool> HasBrandAsync(CategoryId id, CancellationToken ct = default);

    Task AddAsync(Aggregates.Category category, CancellationToken ct = default);

    void Update(Aggregates.Category category, byte[]? rowVersion = null);

    void SetOriginalRowVersion(Aggregates.Category entity, byte[] rowVersion);
}
