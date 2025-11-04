namespace MainApi.Services.Product;

public interface IProductService
{
    Task<(IEnumerable<PublicProductViewDto> products, int totalItems)> GetProductsAsync(ProductSearchDto search);
    Task<object?> GetProductByIdAsync(int id, bool isAdmin);
    Task<TProducts> CreateProductAsync(ProductDto productDto);
    Task<bool> UpdateProductAsync(int id, ProductDto productDto);
    Task<(bool success, string? message)> DeleteProductAsync(int id);
    Task<(bool success, int? newCount, string? message)> AddStockAsync(int id, ProductStockDto stockDto);
    Task<(bool success, int? newCount, string? message)> RemoveStockAsync(int id, ProductStockDto stockDto);
    Task<IEnumerable<object>> GetLowStockProductsAsync(int threshold = 5);
    Task<object> GetProductStatisticsAsync();
    Task<(int updatedCount, string? message)> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice);
    Task<(IEnumerable<object> products, int totalItems)> GetDiscountedProductsAsync(int page, int pageSize, int minDiscount, int maxDiscount, int categoryId);
    Task<(bool success, object? result, string? message)> SetProductDiscountAsync(int id, SetDiscountDto discountDto);
    Task<(bool success, string? message)> RemoveProductDiscountAsync(int id);
    Task<object> GetDiscountStatisticsAsync();
}