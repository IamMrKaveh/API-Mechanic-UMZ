namespace Infrastructure.Persistence.Repositories;

public class OrderStatusRepository : IOrderStatusRepository
{
    private readonly LedkaContext _context;

    public OrderStatusRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync()
    {
        return await _context.Set<OrderStatus>().AsNoTracking().OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _context.Set<OrderStatus>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }
    public async Task<OrderStatus?> GetOrderStatusByIdForUpdateAsync(int id)
    {
        return await _context.Set<OrderStatus>().FindAsync(id);
    }

    public Task<OrderStatus?> GetStatusByNameAsync(string name)
    {
        return _context.Set<OrderStatus>()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task AddOrderStatusAsync(OrderStatus status)
    {
        await _context.Set<OrderStatus>().AddAsync(status);
    }
    public void UpdateOrderStatus(OrderStatus status)
    {
        _context.Set<OrderStatus>().Update(status);
    }

    public async Task<bool> IsOrderStatusInUseAsync(int id)
    {
        return await _context.Set<Domain.Order.Order>().AnyAsync(o => o.OrderStatusId == id);
    }

    public void DeleteOrderStatus(OrderStatus status)
    {
        status.IsDeleted = true;
        status.DeletedAt = DateTime.UtcNow;
        _context.Set<OrderStatus>().Update(status);
    }
}