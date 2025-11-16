namespace Application.Common.Interfaces.Persistence;

public interface IProductRepository
{
    IQueryable<Product> GetProductsQuery(ProductSearchDto search);
    Task<Product?> GetProductByIdAsync(int id, bool isAdmin = false);
    Task<int> GetProductCountAsync(IQueryable<Product> query);
    Task<List<Product>> GetPaginatedProductsAsync(IQueryable<Product> query, int page, int pageSize);
    Task AddProductAsync(Product product);
    void UpdateProduct(Product product);
    void DeleteProduct(Product product);
    Task<bool> HasOrderHistoryAsync(int productId);
    Task<List<ProductVariant>> GetLowStockVariantsAsync(int threshold);
    Task<int> GetActiveProductCountAsync();
    Task<decimal> GetTotalInventoryValueAsync();
    Task<int> GetOutOfStockCountAsync();
    Task<int> GetLowStockCountAsync(int threshold);
    Task<List<ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds);
    IQueryable<ProductVariant> GetDiscountedVariantsQuery(int minDiscount, int maxDiscount, int categoryId);
    Task<int> GetDiscountedVariantsCountAsync(IQueryable<ProductVariant> query);
    Task<List<ProductVariant>> GetPaginatedDiscountedVariantsAsync(IQueryable<ProductVariant> query, int page, int pageSize);
    Task<ProductVariant?> GetVariantByIdAsync(int id);
    Task<int> GetTotalDiscountedVariantsCountAsync();
    Task<double> GetAverageDiscountPercentageAsync();
    Task<long> GetTotalDiscountValueAsync();
    Task<List<object>> GetDiscountStatsByCategoryAsync();
}