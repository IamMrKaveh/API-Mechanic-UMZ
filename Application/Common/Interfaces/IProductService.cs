namespace Application.Common.Interfaces;

public interface IProductService
{
    Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto search);
    Task<ServiceResult<object?>> GetProductByIdAsync(int id, bool isAdmin);
    Task<ServiceResult<Domain.Product.Product>> CreateProductAsync(ProductDto productDto, int userId);
    Task<ServiceResult> UpdateProductAsync(int id, ProductDto productDto, int userId);
    Task<ServiceResult> DeleteProductAsync(int id);
    Task<ServiceResult<(int? newCount, string? message)>> AddStockAsync(int id, ProductStockDto stockDto, int userId);
    Task<ServiceResult<(int? newCount, string? message)>> RemoveStockAsync(int id, ProductStockDto stockDto, int userId);
    Task<ServiceResult<IEnumerable<object>>> GetLowStockProductsAsync(int threshold = 5);
    Task<ServiceResult<object>> GetProductStatisticsAsync();
    Task<ServiceResult<(int updatedCount, string? message)>> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice);
    Task<ServiceResult<PagedResultDto<object>>> GetDiscountedProductsAsync(int page, int pageSize, int minDiscount, int maxDiscount, int categoryId);
    Task<ServiceResult<(object? result, string? message)>> SetProductDiscountAsync(int id, SetDiscountDto discountDto);
    Task<ServiceResult> RemoveProductDiscountAsync(int id);
    Task<ServiceResult<object>> GetDiscountStatisticsAsync();
}