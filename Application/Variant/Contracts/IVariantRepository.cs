namespace Application.Variant.Contracts;

public interface IVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<ProductVariant?> GetByIdForUpdateAsync(int id, CancellationToken ct = default);

    Task<ProductVariant?> GetWithProductAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByProductIdAsync(int productId, CancellationToken ct = default);

    Task<IReadOnlyList<ProductVariant>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    Task AddAsync(ProductVariant variant, CancellationToken ct = default);

    void Update(ProductVariant variant);

    void SetOriginalRowVersion(ProductVariant entity, byte[] rowVersion);
}