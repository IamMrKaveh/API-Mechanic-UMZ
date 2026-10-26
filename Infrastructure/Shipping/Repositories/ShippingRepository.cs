using Domain.Shipping.Interfaces;
using Domain.Shipping.ValueObjects;

using Infrastructure.Persistence.Repositories;

namespace Infrastructure.Shipping.Repositories;

public sealed class ShippingRepository(DBContext context)
    : RepositoryBase<Domain.Shipping.Aggregates.Shipping, ShippingId>(context), IShippingRepository
{
    public async Task<ICollection<Domain.Shipping.Aggregates.Shipping>> GetAllAsync(
        bool includeInactive = false, CancellationToken ct = default)
    {
        // A requested deletion also deactivates the row, so the soft-delete
        // filter is redundant here: filtering on IsActive covers both states.
        var query = Context.Shippings.IgnoreQueryFilters().AsQueryable();
        if (includeInactive is false)
            query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.SortOrder).ToListAsync(ct);
    }

    public override Task<Domain.Shipping.Aggregates.Shipping?> GetByIdAsync(
        ShippingId id, CancellationToken ct = default)
        => Context.Shippings.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id.Equals(id), ct);

    public async Task<ICollection<Domain.Shipping.Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<ShippingId> ids, CancellationToken ct = default)
    {
        var idValues = ids.Select(id => id).ToList();
        return await Context.Shippings
            .Where(s => idValues.Contains(s.Id))
            .ToListAsync(ct);
    }

    public async Task<Domain.Shipping.Aggregates.Shipping?> GetDefaultAsync(CancellationToken ct = default)
        => await Context.Shippings.FirstOrDefaultAsync(s => s.IsDefault && s.IsActive, ct);

    public async Task<bool> ExistsByNameAsync(
        ShippingName shippingName, ShippingId? excludeId = null, CancellationToken ct = default)
    {
        var query = Context.Shippings.Where(s => s.Name == shippingName);
        if (excludeId is not null)
            query = query.Where(s => s.Id != excludeId);
        return await query.AnyAsync(ct);
    }

}