using Domain.Common.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Domain.Shipping.Interfaces;

public interface IShippingRepository : IRepository<Aggregates.Shipping, ShippingId>
{
    Task<ICollection<Aggregates.Shipping>> GetAllAsync(
        bool includeInactive = false,
        CancellationToken ct = default);

    Task<ICollection<Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<ShippingId> ids,
        CancellationToken ct = default);

    Task<Aggregates.Shipping?> GetDefaultAsync(
        CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(
        ShippingName shippingName,
        ShippingId? excludeId = null,
        CancellationToken ct = default);
}