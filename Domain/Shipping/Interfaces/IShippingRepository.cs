using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Interfaces;

public interface IShippingRepository
{
    Task<IReadOnlyList<Aggregates.Shipping>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<Aggregates.Shipping?> GetByIdAsync(
        ShippingId id,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<ShippingId> ids,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Shipping>> GetAllActiveAsync(
        CancellationToken ct = default);

    Task<Aggregates.Shipping?> GetDefaultAsync(
        CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(
        string name,
        ShippingId? excludeId = null,
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Shipping shipping,
        CancellationToken ct = default);

    void Update(Aggregates.Shipping shipping);

    void SetOriginalRowVersion(
        Aggregates.Shipping shipping,
        byte[] rowVersion);
}