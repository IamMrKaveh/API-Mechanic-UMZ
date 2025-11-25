namespace Application.Common.Interfaces.Persistence;

public interface IProductVariantRepository
{
    Task<ProductVariant?> GetByIdAsync(int id, bool includeProduct = false);
    Task<ProductVariant?> GetByIdForUpdateAsync(int id);
    void Update(ProductVariant variant);
    void SetOriginalRowVersion(ProductVariant variant, byte[] rowVersion);
}