namespace Application.Product.Contracts;

public interface IProductRepository
{
    Task<Domain.Product.Product?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Domain.Product.Product?> GetByIdWithVariantsAsync(int id, CancellationToken ct = default);

    Task<Domain.Product.Product?> GetByIdWithAllDetailsAsync(int id, CancellationToken ct = default);

    Task<Domain.Product.Product?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(string sku, int? excludeProductId = null, CancellationToken ct = default);

    Task AddAsync(Domain.Product.Product product, CancellationToken ct = default);

    void Update(Domain.Product.Product product);

    Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default);

    Task<ProductVariant?> GetVariantWithProductAsync(int variantId, CancellationToken ct = default);

    void SetOriginalRowVersion(Domain.Product.Product entity, byte[] rowVersion);

    void SetVariantOriginalRowVersion(ProductVariant entity, byte[] rowVersion);

    void UpdateVariant(object variant);

    Task<ProductVariant> GetVariantByIdForUpdateAsync(int variantId);

    Task<IReadOnlyList<ProductVariant>> GetVariantsByIdsAsync(
        IEnumerable<int> variantIds,
        CancellationToken cancellationToken);
}