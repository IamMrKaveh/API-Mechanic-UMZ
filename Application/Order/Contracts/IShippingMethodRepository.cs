namespace Application.Order.Contracts;

public interface IShippingMethodRepository
{
    Task<IEnumerable<ShippingMethod>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default);

    Task<ShippingMethod?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<List<ShippingMethod>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    Task AddAsync(ShippingMethod method, CancellationToken ct = default);

    void Update(ShippingMethod method);

    void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    Task<IEnumerable<ShippingMethod>> GetAllActiveAsync(CancellationToken ct = default);

    Task<ShippingMethod?> GetDefaultAsync(CancellationToken ct = default);
}