using Domain.Common.Interfaces;
using Domain.Product.ValueObjects;

namespace Domain.Product.Interfaces;

public interface IProductRepository : IRepository<Aggregates.Product, ProductId>
{
    void Update(Aggregates.Product product, byte[]? rowVersion = null);

    void SetOriginalRowVersion(
        Aggregates.Product entity,
        byte[] rowVersion);

    Task<bool> ExistsBySlugAsync(
        ProductSlug slug,
        ProductId? excludeId = null,
        CancellationToken ct = default);
}
