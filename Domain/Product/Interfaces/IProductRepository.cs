using Domain.Product.ValueObjects;

namespace Domain.Product.Interfaces;

public interface IProductRepository
{
    Task AddAsync(
        Aggregates.Product product,
        CancellationToken ct = default);

    void Update(Aggregates.Product product, byte[]? rowVersion = null);

    void SetOriginalRowVersion(
        Aggregates.Product entity,
        byte[] rowVersion);

    Task<Aggregates.Product?> GetByIdAsync(
        ProductId id,
        CancellationToken ct = default);

    Task<bool> ExistsBySlugAsync(
        ProductSlug slug,
        ProductId? excludeId = null,
        CancellationToken ct = default);
}
