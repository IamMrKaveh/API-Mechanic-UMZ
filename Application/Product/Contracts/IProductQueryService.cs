namespace Application.Product.Contracts;

public interface IProductQueryService
{
    Task<ProductDto?> GetByIdAsync(int id);

    Task<IEnumerable<ProductDto>> GetAllAsync();

    Task<AdminProductDetailDto?> GetAdminProductDetailAsync(int productId, CancellationToken ct = default);

    Task<PaginatedResult<AdminProductListItemDto>> GetAdminProductsAsync(AdminProductSearchParams searchParams, CancellationToken ct = default);

    Task<PublicProductDetailDto?> GetPublicProductDetailAsync(int productId, CancellationToken ct = default);

    Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(ProductCatalogSearchParams searchParams, CancellationToken ct = default);

    Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(int productId, bool activeOnly, CancellationToken ct = default);
}