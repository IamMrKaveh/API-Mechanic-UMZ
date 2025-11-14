namespace Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    IQueryable<Domain.Product.Product> GetProductsQuery(
        string? name,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        bool? inStock,
        bool? hasDiscount,
        bool? isUnlimited,
        ProductSortOptions sortBy);
    Task<Domain.Product.Product?> GetProductByIdAsync(int id);
    Task<int> GetProductCountAsync(IQueryable<Domain.Product.Product> query);
    Task<List<Domain.Product.Product>> GetPaginatedProductsAsync(IQueryable<Domain.Product.Product> query, int page, int pageSize);
    Task AddProductAsync(Domain.Product.Product product);
    void UpdateProduct(Domain.Product.Product product);
    void DeleteProduct(Domain.Product.Product product);
    Task<bool> HasOrderHistoryAsync(int productId);
    Task<List<Domain.Product.ProductVariant>> GetLowStockVariantsAsync(int threshold);
    Task<int> GetActiveProductCountAsync();
    Task<decimal> GetTotalInventoryValueAsync();
    Task<int> GetOutOfStockCountAsync();
    Task<int> GetLowStockCountAsync(int threshold);
    Task<List<Domain.Product.ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds);
    IQueryable<Domain.Product.ProductVariant> GetDiscountedVariantsQuery(int minDiscount, int maxDiscount, int categoryId);
    Task<int> GetDiscountedVariantsCountAsync(IQueryable<Domain.Product.ProductVariant> query);
    Task<List<Domain.Product.ProductVariant>> GetPaginatedDiscountedVariantsAsync(IQueryable<Domain.Product.ProductVariant> query, int page, int pageSize);
    Task<Domain.Product.ProductVariant?> GetVariantByIdAsync(int id);
    Task<int> GetTotalDiscountedVariantsCountAsync();
    Task<double> GetAverageDiscountPercentageAsync();
    Task<long> GetTotalDiscountValueAsync();
    Task<List<object>> GetDiscountStatsByCategoryAsync();
}