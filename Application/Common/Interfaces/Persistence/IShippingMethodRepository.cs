namespace Application.Common.Interfaces.Persistence;

public interface IShippingMethodRepository : IGenericRepository<ShippingMethod>
{
    Task<List<ShippingMethod>> GetShippingMethodsAsync(bool includeDeleted);
    Task<List<ShippingMethod>> GetActiveShippingMethodsAsync();
}