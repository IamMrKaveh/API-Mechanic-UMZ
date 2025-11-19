namespace Application.Common.Interfaces;

public interface IAdminProductService
{
    Task<ServiceResult<AdminProductViewDto?>> GetAdminProductByIdAsync(int productId);
    Task<ServiceResult<AdminProductViewDto>> CreateProductAsync(ProductDto productDto, int userId);
    Task<ServiceResult> UpdateProductAsync(int productId, ProductDto productDto, int userId);
    Task<ServiceResult> AddStockAsync(int variantId, int quantity, int userId, string notes);
    Task<ServiceResult> RemoveStockAsync(int variantId, int quantity, int userId, string notes);
    Task<ServiceResult> SetDiscountAsync(int variantId, decimal originalPrice, decimal discountedPrice, int userId);
    Task<ServiceResult> RemoveDiscountAsync(int variantId, int userId);
    Task<ServiceResult> BulkUpdatePricesAsync(Dictionary<int, decimal> priceUpdates, bool isPurchasePrice, int userId);
    Task<ServiceResult> DeleteProductAsync(int productId, int userId);
    Task<ServiceResult<List<AttributeTypeWithValuesDto>>> GetAllAttributesWithValuesAsync();
    Task<ServiceResult<object>> GetProductStatisticsAsync();
    Task<ServiceResult<IEnumerable<object>>> GetLowStockProductsAsync(int threshold);
}