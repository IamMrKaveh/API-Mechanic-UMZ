namespace Application.Shipping.Contracts;

public interface IShippingRepository
{
    Task<IEnumerable<Domain.Shipping.Shipping>> GetAllAsync(bool includeDeleted = false, CancellationToken ct = default);

    Task<Domain.Shipping.Shipping?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<List<Domain.Shipping.Shipping>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default);

    Task AddAsync(Domain.Shipping.Shipping method, CancellationToken ct = default);

    void Update(Domain.Shipping.Shipping method);

    void SetOriginalRowVersion(Domain.Shipping.Shipping method, byte[] rowVersion);

    Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default);

    Task<IEnumerable<Domain.Shipping.Shipping>> GetAllActiveAsync(CancellationToken ct = default);

    Task<Domain.Shipping.Shipping?> GetDefaultAsync(CancellationToken ct = default);
}