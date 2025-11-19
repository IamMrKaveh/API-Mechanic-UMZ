namespace Application.Common.Interfaces;

public interface IShippingService
{
    Task<IEnumerable<object>> GetActiveShippingMethodsAsync();
}