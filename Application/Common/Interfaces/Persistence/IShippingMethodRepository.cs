namespace Application.Common.Interfaces.Persistence;

public interface IShippingMethodRepository
{
    Task<IEnumerable<ShippingMethod>> GetAllAsync(bool includeDeleted = false);
    Task<ShippingMethod?> GetByIdAsync(int id);
    Task<ShippingMethod?> GetByIdIncludingDeletedAsync(int id);
    Task AddAsync(ShippingMethod shippingMethod);
    void Update(ShippingMethod shippingMethod);
    void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
}