namespace Infrastructure.Persistence.Repositories;

public class ShippingMethodRepository : GenericRepository<ShippingMethod>, IShippingMethodRepository
{
    public ShippingMethodRepository(LedkaContext context) : base(context)
    {
    }

    public async Task<List<ShippingMethod>> GetShippingMethodsAsync(bool includeDeleted)
    {
        var query = _context.ShippingMethods.AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(sm => !sm.IsDeleted);
        }

        return await query.OrderBy(sm => sm.Name).ToListAsync();
    }

    public async Task<List<ShippingMethod>> GetActiveShippingMethodsAsync()
    {
        return await _context.ShippingMethods
            .Where(sm => !sm.IsDeleted && sm.IsActive)
            .OrderBy(sm => sm.Name)
            .ToListAsync();
    }
}