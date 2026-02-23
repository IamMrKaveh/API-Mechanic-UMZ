namespace Infrastructure.Shipping.Repositories;

public class ShippingMethodRepository : IShippingRepository
{
    private readonly Persistence.Context.DBContext _context;

    public ShippingMethodRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Domain.Shipping.Shipping>> GetAllAsync(
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

    public async Task<Domain.Shipping.Shipping?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Shippings
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<List<Domain.Shipping.Shipping>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.Shippings
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(Domain.Shipping.Shipping method, CancellationToken ct = default)
    {
        await _context.Shippings.AddAsync(method, ct);
    }

    public void Update(Domain.Shipping.Shipping method)
    {
        _context.Shippings.Update(method);
    }

    public void SetOriginalRowVersion(Domain.Shipping.Shipping method, byte[] rowVersion)
    {
        _context.Entry(method).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.Shippings
            .Where(m => m.Name == name.Trim() && !m.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(m => m.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public async Task<IEnumerable<Domain.Shipping.Shipping>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Shippings.Where(m => m.IsActive && !m.IsDeleted).OrderBy(m => m.SortOrder).ToListAsync(ct);
    }

    public async Task<Domain.Shipping.Shipping?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await _context.Shippings.FirstOrDefaultAsync(m => m.IsDefault && !m.IsDeleted, ct);
    }
}