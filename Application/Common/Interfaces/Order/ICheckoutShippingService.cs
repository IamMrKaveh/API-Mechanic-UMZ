namespace Application.Common.Interfaces.Order;

public interface ICheckoutShippingService
{
    Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForCartAsync(int userId);
    Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForVariantsAsync(IEnumerable<int> variantIds);
}