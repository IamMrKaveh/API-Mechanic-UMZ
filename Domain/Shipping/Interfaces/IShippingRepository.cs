using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Interfaces;

public interface IShippingRepository
{
    Task<ICollection<Aggregates.Shipping>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<Aggregates.Shipping?> GetByIdAsync(
        ShippingId id,
        CancellationToken ct = default);

    Task<ICollection<Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<ShippingId> ids,
        CancellationToken ct = default);

    Task<ICollection<Aggregates.Shipping>> GetAllActiveAsync(
        CancellationToken ct = default);

    Task<Aggregates.Shipping?> GetDefaultAsync(
        CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(
        ShippingName shippingName,
        ShippingId? excludeId = null,
        CancellationToken ct = default);

    Task AddAsync(
        Aggregates.Shipping shipping,
        CancellationToken ct = default);

    void Update(Aggregates.Shipping shipping);
}