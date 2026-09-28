using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Common.Interfaces;

namespace Domain.Brand.Interfaces;

public interface IBrandRepository : IRepository<Aggregates.Brand, BrandId>
{
    void Update(Aggregates.Brand brand, byte[]? rowVersion = null);

    void SetOriginalRowVersion(
        Aggregates.Brand entity,
        byte[] rowVersion);

    byte[]? GetCurrentRowVersion(Aggregates.Brand entity);

    Task<bool> ExistsByNameInCategoryAsync(
        BrandName brandName,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        BrandSlug slug,
        BrandId? excludeId = null,
        CancellationToken ct = default);
}
