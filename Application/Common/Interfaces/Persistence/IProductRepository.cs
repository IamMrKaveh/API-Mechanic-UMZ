namespace Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    Task<(List<Product> Products, int TotalItems)> GetPagedAsync(ProductSearchDto searchParams);
    Task<Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeAll = false);
    Task<Product?> GetByIdWithCategoryAndMediaAsync(int productId);
    Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids);
    Task<List<AttributeType>> GetAllAttributeTypesWithValuesAsync();
    Task AddAsync(Product product);
    void UpdateVariants(Product product, List<CreateProductVariantDto> variantDtos);
    void SetOriginalRowVersion(Product product, byte[] rowVersion);
    void Update(Product product);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    void UpdateVariant(ProductVariant variant);
    Task<Dictionary<int, ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds);
    Task<bool> SkuExistsAsync(string sku, int? variantId = null);
    Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold);
    Task<object> GetProductStatisticsAsync();
}