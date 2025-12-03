namespace Application.Common.Interfaces.Admin;

public interface IAdminShippingMethodService
{
    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetShippingMethodsAsync(bool includeDeleted = false);
    Task<ServiceResult<ShippingMethodDto?>> GetShippingMethodByIdAsync(int id);
    Task<ServiceResult<ShippingMethodDto>> CreateShippingMethodAsync(ShippingMethodCreateDto dto, int currentUserId);
    Task<ServiceResult> UpdateShippingMethodAsync(int id, ShippingMethodUpdateDto dto, int currentUserId);
    Task<ServiceResult> DeleteShippingMethodAsync(int id, int currentUserId);
    Task<ServiceResult> RestoreShippingMethodAsync(int id, int currentUserId);
}