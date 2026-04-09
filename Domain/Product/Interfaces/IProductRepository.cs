using Domain.Brand.ValueObjects;
using Domain.Category.ValueObjects;
using Domain.Product.ValueObjects;

namespace Domain.Product.Interfaces;

public interface IProductRepository
{
    Task AddAsync(
        Aggregates.Product product,
        CancellationToken ct = default);

    void Update(Aggregates.Product product);

    void SetOriginalRowVersion(
        Aggregates.Product entity,
        byte[] rowVersion);

    Task<Aggregates.Product?> GetByIdAsync(
        ProductId id,
        CancellationToken ct = default);

    Task<Aggregates.Product?> GetBySlugAsync(
        Slug slug,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(
        ProductId id,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        Slug slug,
        ProductId? excludeId = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Product>> GetByCategoryIdAsync(
        CategoryId categoryId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Product>> GetByBrandIdAsync(
        BrandId brandId,
        CancellationToken ct = default);
}