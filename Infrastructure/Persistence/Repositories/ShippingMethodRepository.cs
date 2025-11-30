namespace Infrastructure.Persistence.Repositories;

public class ShippingMethodRepository : IShippingMethodRepository
{
    private readonly LedkaContext _context;

    public ShippingMethodRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ShippingMethod>> GetAllAsync(bool includeDeleted = false)
    {
        var query = _context.ShippingMethods.AsQueryable();

        if (includeDeleted)
        {
            query = query.IgnoreQueryFilters();
        }

        return await query.OrderBy(s => s.Name).ToListAsync();
    }

    public async Task<ShippingMethod?> GetByIdAsync(int id)
    {
        return await _context.ShippingMethods.FindAsync(id);
    }

    public async Task<ShippingMethod?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _context.ShippingMethods
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.ShippingMethods.Where(s => s.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(s => s.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task AddAsync(ShippingMethod method)
    {
        await _context.ShippingMethods.AddAsync(method);
    }

    public void Update(ShippingMethod method)
    {
        _context.ShippingMethods.Update(method);
    }

    public void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion)
    {
        _context.Entry(method).Property(s => s.RowVersion).OriginalValue = rowVersion;
    }
}