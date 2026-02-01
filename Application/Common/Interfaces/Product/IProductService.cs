using Application.DTOs.Product;

namespace Application.Common.Interfaces.Product;

public interface IProductService
{
    Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto searchDto);
    Task<ServiceResult<PublicProductViewDto?>> GetProductByIdAsync(int productId, bool includeInactive = false);
    Task<ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>> GetAllAttributesAsync();
}