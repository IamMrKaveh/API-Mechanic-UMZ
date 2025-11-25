namespace Infrastructure.Persistence.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly LedkaContext _context;

    public OrderItemRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        return await _context.OrderItems
            .Where(oi => oi.OrderId == orderId)
            .Include(oi => oi.Variant.Product)
            .ToListAsync();
    }

    public async Task<OrderItem?> GetOrderItemByIdAsync(int orderItemId)
    {
        return await _context.OrderItems
            .AsNoTracking()
            .Include(oi => oi.Variant.Product)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
    }

    public async Task<OrderItem?> GetOrderItemByIdForUpdateAsync(int orderItemId)
    {
        return await _context.OrderItems
            .Include(oi => oi.Variant)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
    }

    public async Task AddOrderItemAsync(OrderItem orderItem)
    {
        await _context.OrderItems.AddAsync(orderItem);
    }

    public void SetOrderItemRowVersion(OrderItem item, byte[] rowVersion)
    {
        _context.Entry(item).Property(i => i.RowVersion).OriginalValue = rowVersion;
    }

    public void RemoveOrderItem(OrderItem item)
    {
        _context.OrderItems.Remove(item);
    }
}