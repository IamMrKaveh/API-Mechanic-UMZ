using Domain.Common.Interfaces;
using Domain.Product.ValueObjects;
using Domain.Variant.Aggregates;
using Domain.Variant.ValueObjects;

namespace Domain.Variant.Interfaces;

public interface IVariantRepository : IRepository<ProductVariant, VariantId>
{
    Task<ProductVariant?> GetForUpdateAsync(
        VariantId id,
        CancellationToken ct = default);

    Task<ProductVariant?> GetWithProductAsync(
        VariantId id,
        CancellationToken ct = default);

    Task<ProductVariant?> GetVariantWithShippingsAsync(
        VariantId id,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(
        IEnumerable<VariantId> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByIdsWithShippingsAsync(
        IEnumerable<VariantId> ids,
        CancellationToken ct = default);

    Task<bool> ExistsAsync(VariantId id, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(
        Sku sku,
        VariantId? excludeId = null,
        CancellationToken ct = default);

    Task<bool> ExistsByAttributeCombinationAsync(
        ProductId productId,
        IReadOnlyCollection<Guid> attributeValueIdsSorted,
        VariantId? excludeId,
        CancellationToken ct = default);
}