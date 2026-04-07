using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Interfaces;

public interface IVariantRepository
{
    Task AddAsync(ProductVariant variant, CancellationToken ct = default);

    void Update(ProductVariant variant);

    void SetOriginalRowVersion(ProductVariant entity, byte[] rowVersion);

    Task<ProductVariant?> GetByIdAsync(VariantId id, CancellationToken ct = default);

    Task<ProductVariant?> GetByIdForUpdateAsync(VariantId id, CancellationToken ct = default);

    Task<ProductVariant?> GetWithProductAsync(VariantId id, CancellationToken ct = default);

    Task<ProductVariant?> GetVariantWithShippingsAsync(VariantId id, CancellationToken ct = default);

    Task<ProductVariant?> GetBySkuAsync(string sku, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(ProductId productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetActiveByProductIdAsync(ProductId productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(IEnumerable<VariantId> ids, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(string sku, VariantId? excludeId = null, CancellationToken ct = default);

    Task<bool> ExistsAsync(VariantId id, CancellationToken ct = default);
}