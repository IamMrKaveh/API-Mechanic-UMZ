namespace Infrastructure.Persistence.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly LedkaContext _context;

    public OrderItemRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Order.OrderItem> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize)
    {
        var query = _context.Set<Domain.Order.OrderItem>()
            .Include(oi => oi.Variant.Product)
            .Include(oi => oi.Order)
            .AsQueryable();

        if (orderId.HasValue)
        {
            query = query.Where(oi => oi.OrderId == orderId.Value);
        }

        if (!isAdmin)
        {
            query = query.Where(oi => oi.Order != null && oi.Order.UserId == currentUserId);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(oi => oi.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Domain.Order.OrderItem?> GetOrderItemByIdAsync(int orderItemId)
    {
        return await _context.Set<Domain.Order.OrderItem>()
            .AsNoTracking()
            .Include(oi => oi.Variant.Product.CategoryGroup.Category)
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
    }

    public async Task<Domain.Order.OrderItem?> GetOrderItemWithDetailsAsync(int orderItemId)
    {
        return await _context.Set<Domain.Order.OrderItem>()
            .Include(oi => oi.Variant.Product)
            .Include(oi => oi.Order)
            .FirstOrDefaultAsync(oi => oi.Id == orderItemId);
    }


    public async Task<Domain.Product.ProductVariant?> GetProductVariantWithProductAsync(int variantId)
    {
        return await _context.Set<Domain.Product.ProductVariant>()
            .Include(v => v.Product)
            .FirstOrDefaultAsync(v => v.Id == variantId);
    }

    public async Task AddOrderItemAsync(Domain.Order.OrderItem orderItem)
    {
        await _context.Set<Domain.Order.OrderItem>().AddAsync(orderItem);
    }

    public void SetOrderItemRowVersion(Domain.Order.OrderItem item, byte[] rowVersion)
    {
        _context.Entry(item).Property(p => p.RowVersion).OriginalValue = rowVersion;
    }

    public void DeleteOrderItem(Domain.Order.OrderItem item)
    {
        _context.Set<Domain.Order.OrderItem>().Remove(item);
    }
}