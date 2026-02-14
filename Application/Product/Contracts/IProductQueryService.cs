namespace Application.Product.Contracts;

/// <summary>
/// Read-side query service for products.
/// Returns DTOs directly — no domain entities.
/// Can use optimized SQL, views, or Elasticsearch.
/// </summary>
public interface IProductQueryService
{
    /// <summary>
    /// Admin: paginated product list with filters
    /// </summary>
    Task<PaginatedResult<AdminProductListItemDto>> GetAdminProductsAsync(
        AdminProductSearchParams searchParams,
        CancellationToken ct = default);

    /// <summary>
    /// Admin: full product detail with variants
    /// </summary>
    Task<AdminProductDetailDto?> GetAdminProductDetailAsync(
        int productId,
        CancellationToken ct = default);

    /// <summary>
    /// Public: product catalog listing
    /// </summary>
    Task<PaginatedResult<ProductCatalogItemDto>> GetProductCatalogAsync(
        ProductCatalogSearchParams searchParams,
        CancellationToken ct = default);

    /// <summary>
    /// Public: full product detail for product page
    /// </summary>
    Task<PublicProductDetailDto?> GetPublicProductDetailAsync(
        int productId,
        CancellationToken ct = default);

    /// <summary>
    /// Public: variants of a product
    /// </summary>
    Task<IEnumerable<ProductVariantViewDto>> GetProductVariantsAsync(
        int productId,
        bool activeOnly = true,
        CancellationToken ct = default);

    Task<AdminProductViewDto?> GetProductForAdminAsync(int productId, CancellationToken ct = default);

    Task<PublicProductViewDto?> GetProductForPublicAsync(int productId, CancellationToken ct = default);
}