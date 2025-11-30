namespace Application.Common.Interfaces;

public interface IShippingMethodService
{
    Task<IEnumerable<ShippingMethodDto>> GetAllAsync(bool includeDeleted = false);
    Task<ShippingMethodDto?> GetByIdAsync(int id);
    Task<ServiceResult<ShippingMethodDto>> CreateAsync(ShippingMethodCreateDto dto);
    Task<ServiceResult> UpdateAsync(int id, ShippingMethodUpdateDto dto);
    Task<ServiceResult> DeleteAsync(int id);
    Task<ServiceResult> RestoreAsync(int id);
    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync();
}