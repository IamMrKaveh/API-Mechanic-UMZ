using Infrastructure.Persistence.Interface.Order;

namespace Infrastructure.Persistence.Repositories.Order;

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

        if (!includeDeleted)
        {
            query = query.Where(sm => !sm.IsDeleted);
        }

        return await query.OrderBy(sm => sm.Name).ToListAsync();
    }

    public async Task<ShippingMethod?> GetByIdAsync(int id)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(sm => sm.Id == id && !sm.IsDeleted);
    }

    public async Task<ShippingMethod?> GetByIdIncludingDeletedAsync(int id)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(sm => sm.Id == id);
    }

    public async Task AddAsync(ShippingMethod shippingMethod)
    {
        await _context.ShippingMethods.AddAsync(shippingMethod);
    }

    public void Update(ShippingMethod shippingMethod)
    {
        _context.ShippingMethods.Update(shippingMethod);
    }

    public void SetOriginalRowVersion(ShippingMethod method, byte[] rowVersion)
    {
        _context.Entry(method).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
    {
        var query = _context.ShippingMethods
            .Where(sm => sm.Name == name && !sm.IsDeleted);

        if (excludeId.HasValue)
        {
            query = query.Where(sm => sm.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}