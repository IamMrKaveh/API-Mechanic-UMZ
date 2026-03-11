using Domain.Shipping.Interfaces;

namespace Infrastructure.Shipping.Repositories;

public class ShippingRepository(DBContext context) : IShippingRepository
{
    private readonly DBContext _context = context;

    public async Task<IEnumerable<Domain.Shipping.Aggregates.Shipping>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _context.Shippings.IgnoreQueryFilters()
            : _context.Shippings.AsQueryable();

        return await query
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<Domain.Shipping.Aggregates.Shipping?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return await _context.Shippings
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<IEnumerable<Domain.Shipping.Aggregates.Shipping>> GetByIdsAsync(
        IEnumerable<int> ids,
        CancellationToken ct = default)
    {
        return await _context.Shippings
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Domain.Shipping.Aggregates.Shipping>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Shippings.Where(m => m.IsActive && !m.IsDeleted).OrderBy(m => m.SortOrder).ToListAsync(ct);
    }

    public async Task<Domain.Shipping.Aggregates.Shipping?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await _context.Shippings.FirstOrDefaultAsync(m => m.IsDefault && !m.IsDeleted, ct);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken ct = default)
    {
        var query = _context.Shippings
            .Where(m => m.Name == name.Trim() && !m.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(m => m.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task AddAsync(
        Domain.Shipping.Aggregates.Shipping shipping,
        CancellationToken ct = default)
    {
        await _context.Shippings.AddAsync(shipping, ct);
    }

    public void Update(Domain.Shipping.Aggregates.Shipping shipping)
    {
        _context.Shippings.Update(shipping);
    }

    public void SetOriginalRowVersion(
        Domain.Shipping.Aggregates.Shipping shipping,
        byte[] rowVersion)
    {
        _context.Entry(shipping).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}