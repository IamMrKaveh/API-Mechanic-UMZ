namespace Application.Common.Interfaces.Persistence;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int id, bool includeProduct = false);
    Task<ProductVariant?> GetByIdForUpdateAsync(int id);
    Task<ProductVariant?> GetBySkuAsync(string sku);
    Task<IEnumerable<ProductVariant>> GetByProductIdAsync(int productId);
    Task<IEnumerable<ProductVariant>> GetLowStockVariantsAsync(int threshold);
    Task<IEnumerable<ProductVariant>> GetOutOfStockVariantsAsync();
    void Update(ProductVariant variant);
    void SetOriginalRowVersion(ProductVariant variant, byte[] rowVersion);
    Task<bool> ExistsAsync(int id);
}