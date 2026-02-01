namespace Infrastructure.Persistence.Interface.Product;

public interface IProductRepository
{
    Task<(List<Domain.Product.Product> Products, int TotalItems)> GetPagedAsync(ProductSearchDto searchParams);
    Task<Domain.Product.Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeAll = false);
    Task<Domain.Product.Product?> GetByIdWithCategoryAndMediaAsync(int productId);
    Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids);
    Task<List<AttributeType>> GetAllAttributeTypesWithValuesAsync();
    Task AddAsync(Domain.Product.Product product);
    void UpdateVariants(Domain.Product.Product product, List<CreateProductVariantDto> variantDtos);
    void SetOriginalRowVersion(Domain.Product.Product product, byte[] rowVersion);
    void Update(Domain.Product.Product product);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    Task<ProductVariant?> GetVariantByIdForUpdateAsync(int variantId);
    void SetVariantRowVersion(ProductVariant variant, byte[] rowVersion);
    void UpdateVariant(ProductVariant variant);
    Task<Dictionary<int, ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds);
    Task<bool> SkuExistsAsync(string sku, int? variantId = null);
    Task<bool> ProductSkuExistsAsync(string sku, int? productId = null);
    Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold);
    Task<object> GetProductStatisticsAsync();
}