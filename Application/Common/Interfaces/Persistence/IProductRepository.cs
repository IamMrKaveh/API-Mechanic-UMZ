namespace Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    Task<(List<Product> products, int totalCount)> GetPagedAsync(ProductSearchDto searchDto);
    Task<Product?> GetByIdWithVariantsAndAttributesAsync(int productId, bool includeInactive = false);
    Task<List<AttributeValue>> GetAttributeValuesByIdsAsync(List<int> ids);
    Task<List<AttributeType>> GetAllAttributesAsync();
    void UpdateVariants(Product product, List<CreateProductVariantDto> variantDtos);
    void SetOriginalRowVersion(Product product, byte[] rowVersion);
    Task<ProductVariant?> GetVariantByIdAsync(int variantId);
    Task AddAsync(Product product);
}