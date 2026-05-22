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

    Task<bool> ExistsBySlugAsync(
        Slug slug,
        ProductId? excludeId = null,
        CancellationToken ct = default);
}