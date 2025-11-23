namespace Application.Common.Interfaces.Persistence;

public interface IShippingMethodRepository
{
    Task<IEnumerable<ShippingMethod>> GetAllAsync(bool includeDeleted);
    Task<ShippingMethod?> GetByIdAsync(int id);
    Task AddAsync(ShippingMethod shippingMethod);
    void Update(ShippingMethod shippingMethod);
    void SetOriginalRowVersion(ShippingMethod shippingMethod, byte[] rowVersion);
}