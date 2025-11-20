namespace Application.Common.Interfaces;

public interface IShippingService
{
    Task<ServiceResult<ShippingMethodDto>> CreateShippingMethodAsync(ShippingMethodCreateDto dto);
    Task<ServiceResult> DeleteShippingMethodAsync(int id);
    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync();
    Task<ServiceResult<ShippingMethodDto?>> GetShippingMethodByIdAsync(int id);
    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetShippingMethodsAsync(bool includeDeleted);
    Task<ServiceResult> RestoreShippingMethodAsync(int id);
    Task<ServiceResult> UpdateShippingMethodAsync(int id, ShippingMethodUpdateDto dto);
}