using Polly;

namespace Infrastructure.Persistence.Repositories;

public class ShippingMethodRepository : IShippingMethodRepository
{
    private LedkaContext _context;

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
        return await query.OrderBy(s => s.Cost).ToListAsync();
    }

    public async Task<ShippingMethod?> GetByIdAsync(int id)
    {
        return await _context.ShippingMethods.FindAsync(id);
    }

    public async Task AddAsync(ShippingMethod shippingMethod)
    {
        await _context.ShippingMethods.AddAsync(shippingMethod);
    }

    public void Update(ShippingMethod shippingMethod)
    {
        _context.ShippingMethods.Update(shippingMethod);
    }

    public void SetOriginalRowVersion(ShippingMethod shippingMethod, byte[] rowVersion)
    {
        _context.Entry(shippingMethod).Property("RowVersion").OriginalValue = rowVersion;
    }
}