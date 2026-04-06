using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;

namespace Domain.Brand.Interfaces;

public interface IBrandRepository
{
    Task AddAsync(
        Domain.Brand.Aggregates.Brand brand,
        CancellationToken ct = default);

    void Update(Domain.Brand.Aggregates.Brand brand);

    void SetOriginalRowVersion(
        Domain.Brand.Aggregates.Brand entity,
        byte[] rowVersion);

    Task<Domain.Brand.Aggregates.Brand?> GetByIdAsync(
        BrandId id,
        CancellationToken ct = default);

    Task<Domain.Brand.Aggregates.Brand?> GetBySlugAsync(
        string slug,
        CancellationToken ct = default);

    Task<bool> ExistsByNameInCategoryAsync(
        string name,
        CategoryId categoryId,
        BrandId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        string slug,
        BrandId? excludeId = null,
        CancellationToken ct = default);
}