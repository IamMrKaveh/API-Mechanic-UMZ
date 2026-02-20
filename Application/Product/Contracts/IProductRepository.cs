namespace Application.Product.Contracts;

public interface IProductRepository
{
    Task<Domain.Product.Product?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Domain.Product.Product?> GetByIdWithAllDetailsAsync(int id, CancellationToken ct = default);

    Task<Domain.Product.Product?> GetByIdIncludingDeletedAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsBySkuAsync(string sku, int? excludeProductId = null, CancellationToken ct = default);

    Task AddAsync(Domain.Product.Product product, CancellationToken ct = default);

    void Update(Domain.Product.Product product);

    void SetOriginalRowVersion(Domain.Product.Product entity, byte[] rowVersion);

    Task<Domain.Product.Product?> GetByIdWithVariantsAsync(int id, CancellationToken ct = default);

    Task<ProductVariant?> GetVariantByIdAsync(int variantId, CancellationToken ct = default);

    Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds, CancellationToken ct = default);
}