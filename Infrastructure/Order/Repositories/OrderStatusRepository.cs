namespace Infrastructure.Order.Repositories;

public class OrderStatusRepository : IOrderStatusRepository
{
    private readonly Persistence.Context.DBContext _context;

    public OrderStatusRepository(Persistence.Context.DBContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderStatus>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>()
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
    }

    public async Task<OrderStatus?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>()
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<OrderStatus?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>()
            .FirstOrDefaultAsync(s => s.Name == name && !s.IsDeleted, ct);
    }

    public async Task<OrderStatus?> GetDefaultStatusAsync(CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>()
            .FirstOrDefaultAsync(s => s.IsDefault && !s.IsDeleted, ct);
    }

    public async Task AddAsync(OrderStatus status, CancellationToken ct = default)
    {
        await _context.Set<OrderStatus>().AddAsync(status, ct);
    }

    public void Update(OrderStatus status)
    {
        _context.Set<OrderStatus>().Update(status);
    }

    public async Task<bool> IsInUseAsync(int id, CancellationToken ct = default)
    {
        
        
        var status = await GetByIdAsync(id, ct);
        if (status == null) return false;

        return await _context.Orders
            .AnyAsync(o => o.Status == OrderStatusValue.FromString(status.Name), ct);
    }

    public async Task<IEnumerable<OrderStatus>> GetAllActiveAsync(CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>().Where(s => s.IsActive && !s.IsDeleted).OrderBy(s => s.SortOrder).ToListAsync(ct);
    }

    public async Task<OrderStatus?> GetDefaultAsync(CancellationToken ct = default)
    {
        return await _context.Set<OrderStatus>().FirstOrDefaultAsync(s => s.IsDefault && !s.IsDeleted, ct);
    }
}