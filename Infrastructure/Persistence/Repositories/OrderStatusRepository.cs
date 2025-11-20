namespace Infrastructure.Persistence.Repositories;

public class OrderStatusRepository : IOrderStatusRepository
{
    private readonly LedkaContext _context;

    public OrderStatusRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync()
    {
        return await _context.Set<Domain.Order.OrderStatus>().AsNoTracking().OrderBy(s => s.Id).ToListAsync();
    }

    public async Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _context.Set<Domain.Order.OrderStatus>().AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
    }
    public async Task<Domain.Order.OrderStatus?> GetOrderStatusByIdForUpdateAsync(int id)
    {
        return await _context.Set<Domain.Order.OrderStatus>().FindAsync(id);
    }

    public Task<Domain.Order.OrderStatus?> GetStatusByNameAsync(string name)
    {
        return _context.Set<Domain.Order.OrderStatus>()
            .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
    }

    public async Task AddOrderStatusAsync(Domain.Order.OrderStatus status)
    {
        await _context.Set<Domain.Order.OrderStatus>().AddAsync(status);
    }
    public void UpdateOrderStatus(Domain.Order.OrderStatus status)
    {
        _context.Set<Domain.Order.OrderStatus>().Update(status);
    }

    public async Task<bool> IsOrderStatusInUseAsync(int id)
    {
        return await _context.Set<Domain.Order.Order>().AnyAsync(o => o.OrderStatusId == id);
    }

    public void DeleteOrderStatus(Domain.Order.OrderStatus status)
    {
        status.IsDeleted = true;
        status.DeletedAt = DateTime.UtcNow;
        _context.Set<Domain.Order.OrderStatus>().Update(status);
    }
}