namespace Application.Common.Interfaces.Admin;

public interface IAdminProductVariantShippingService
{
    Task<ServiceResult<ProductVariantShippingInfoDto>> GetShippingMethodsAsync(int variantId);

    Task<ServiceResult> UpdateShippingMethodsAsync(
        int variantId,
        UpdateProductVariantShippingMethodsDto dto,
        int currentUserId);

    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetAllShippingMethodsAsync();
}