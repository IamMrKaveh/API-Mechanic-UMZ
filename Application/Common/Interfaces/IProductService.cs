namespace Application.Common.Interfaces;

public interface IProductService
{
    Task<ServiceResult<PagedResultDto<PublicProductViewDto>>> GetProductsAsync(ProductSearchDto searchDto);
    Task<ServiceResult<PublicProductViewDto?>> GetProductByIdAsync(int productId, bool includeInactive = false);
    Task<ServiceResult<IEnumerable<AttributeTypeWithValuesDto>>> GetAllAttributesAsync();
}