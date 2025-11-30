namespace Application.Common.Interfaces.Persistence;

public interface IShippingMethodRepository
{
    Task<IEnumerable<ShippingMethod>> GetAllAsync(bool includeDeleted = false);
    Task<ShippingMethod?> GetByIdAsync(int id);
    Task<ShippingMethod?> GetByIdIncludingDeletedAsync(int id);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
    Task AddAsync(ShippingMethod method);
    void Update(ShippingMethod method);
    void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion);
}