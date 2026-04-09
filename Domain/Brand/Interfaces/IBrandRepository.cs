using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandRepository
{
    Task AddAsync(
        Aggregates.Brand brand,
        CancellationToken ct = default);

    void Update(Aggregates.Brand brand);

    void SetOriginalRowVersion(
        Aggregates.Brand entity,
        byte[] rowVersion);

    Task<Aggregates.Brand?> GetByIdAsync(
        BrandId id,
        CancellationToken ct = default);

    Task<Aggregates.Brand?> GetBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<bool> ExistsByNameInCategoryAsync(
        BrandName brandName,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        Slug slug,
        BrandId? excludeId = null,
        CancellationToken ct = default);
}