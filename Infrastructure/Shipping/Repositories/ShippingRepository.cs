using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

namespace Infrastructure.Shipping.Repositories;

public sealed class ShippingRepository(DBContext context) : IShippingRepository
{
    public async Task<ICollection<Domain.Shipping.Aggregates.Shipping>> GetAllAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        var query = context.Shippings.AsQueryable();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.SortOrder).ToListAsync(ct);
    }

    public async Task<Domain.Shipping.Aggregates.Shipping?> GetByIdAsync(ShippingId id, CancellationToken ct = default)
        => await context.Shippings.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<ICollection<Domain.Shipping.Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<ShippingId> ids, CancellationToken ct = default)
    {
        var idValues = ids.Select(id => id.Value).ToList();
        return await context.Shippings
            .Where(s => idValues.Contains(s.Id.Value))
            .ToListAsync(ct);
    }

    public async Task<ICollection<Domain.Shipping.Aggregates.Shipping>> GetAllActiveAsync(CancellationToken ct = default)
        => await context.Shippings
            .Where(s => s.IsActive)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

    public async Task<Domain.Shipping.Aggregates.Shipping?> GetDefaultAsync(CancellationToken ct = default)
        => await context.Shippings.FirstOrDefaultAsync(s => s.IsDefault && s.IsActive, ct);

    public async Task<bool> ExistsByNameAsync(
        ShippingName shippingName, ShippingId? excludeId = null, CancellationToken ct = default)
    {
        var query = context.Shippings.Where(s => s.Name == shippingName);
        if (excludeId is not null)
            query = query.Where(s => s.Id != excludeId);
        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(Domain.Shipping.Aggregates.Shipping shipping, CancellationToken ct = default)
        => await context.Shippings.AddAsync(shipping, ct);

    public void Update(Domain.Shipping.Aggregates.Shipping shipping)
        => context.Shippings.Update(shipping);
}