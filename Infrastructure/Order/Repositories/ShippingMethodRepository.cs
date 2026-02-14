namespace Infrastructure.Order.Repositories;

public class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly LedkaContext _context;

    public ShippingMethodRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ShippingMethod>> GetAllAsync(
        bool includeDeleted = false,
        CancellationToken ct = default)
    {
        var query = includeDeleted
            ? _context.ShippingMethods.IgnoreQueryFilters()
            : _context.ShippingMethods.AsQueryable();

        return await query
            .OrderBy(m => m.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<ShippingMethod?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<List<ShippingMethod>> GetByIdsAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        return await _context.ShippingMethods
            .Where(m => ids.Contains(m.Id))
            .ToListAsync(ct);
    }

    public async Task AddAsync(ShippingMethod method, CancellationToken ct = default)
    {
        await _context.ShippingMethods.AddAsync(method, ct);
    }

    public void Update(ShippingMethod method)
    {
        _context.ShippingMethods.Update(method);
    }

    public void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion)
    {
        _context.Entry(method).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken ct = default)
    {
        var query = _context.ShippingMethods
            .Where(m => m.Name == name.Trim() && !m.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(m => m.Id != excludeId.Value);

        return await query.AnyAsync(ct);
    }

    public Task<IEnumerable<ShippingMethod>> GetAllActiveAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<ShippingMethod?> GetDefaultAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}