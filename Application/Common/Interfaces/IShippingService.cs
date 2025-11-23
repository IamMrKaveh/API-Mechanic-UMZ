namespace Application.Common.Interfaces;

public interface IShippingService
{
    Task<ServiceResult<IEnumerable<ShippingMethodDto>>> GetActiveShippingMethodsAsync();
}