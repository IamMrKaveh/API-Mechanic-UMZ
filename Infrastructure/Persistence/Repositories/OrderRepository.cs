namespace Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly LedkaContext _context;

    public OrderRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Order.Order> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.Set<Domain.Order.Order>()
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
            .AsQueryable();

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (statusId.HasValue)
            query = query.Where(o => o.OrderStatusId == statusId.Value);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        if (!isAdmin)
            query = query.Where(o => o.UserId == currentUserId);

        var totalItems = await query.CountAsync();
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (orders, totalItems);
    }

    public async Task<Domain.Order.Order?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin)
    {
        var query = _context.Set<Domain.Order.Order>()
            .AsNoTracking()
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.ShippingMethod)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.CategoryGroup.Category)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.AttributeType)
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        return await query.FirstOrDefaultAsync();
    }
    public Task<Domain.Order.Order?> GetOrderByIdempotencyKey(string idempotencyKey, int userId)
    {
        return _context.Set<Domain.Order.Order>()
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId);
    }

    public Task<Domain.Order.Order?> GetOrderForPaymentAsync(int orderId)
    {
        return _context.Set<Domain.Order.Order>().FindAsync(orderId).AsTask();
    }
    public Task<Domain.Order.Order?> GetOrderForUpdateAsync(int orderId)
    {
        return _context.Set<Domain.Order.Order>().FindAsync(orderId).AsTask();
    }

    public Task<Domain.Order.Order?> GetOrderWithItemsAsync(int orderId)
    {
        return _context.Set<Domain.Order.Order>()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Dictionary<int, Domain.Product.ProductVariant>> GetVariantsByIdsAsync(List<int> variantIds)
    {
        return await _context.Set<Domain.Product.ProductVariant>()
            .Where(v => variantIds.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id);
    }

    public Task<Domain.Order.ShippingMethod?> GetShippingMethodAsync(int shippingMethodId)
    {
        return _context.Set<Domain.Order.ShippingMethod>().FindAsync(shippingMethodId).AsTask();
    }

    public Task<Domain.Payment.PaymentTransaction?> GetPaymentTransactionAsync(string authority)
    {
        return _context.Set<Domain.Payment.PaymentTransaction>().FirstOrDefaultAsync(t => t.Authority == authority);
    }

    public async Task AddOrderAsync(Domain.Order.Order order)
    {
        await _context.Set<Domain.Order.Order>().AddAsync(order);
    }
    public void UpdateOrder(Domain.Order.Order order)
    {
        _context.Set<Domain.Order.Order>().Update(order);
    }

    public async Task AddDiscountUsageAsync(Domain.Discount.DiscountUsage discountUsage)
    {
        await _context.Set<Domain.Discount.DiscountUsage>().AddAsync(discountUsage);
    }

    public async Task AddPaymentTransactionAsync(Domain.Payment.PaymentTransaction transaction)
    {
        await _context.Set<Domain.Payment.PaymentTransaction>().AddAsync(transaction);
    }

    public void SetOrderRowVersion(Domain.Order.Order order, byte[] rowVersion)
    {
        _context.Entry(order).Property("RowVersion").OriginalValue = rowVersion;
    }

    public void DeleteOrder(Domain.Order.Order order)
    {
        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        _context.Set<Domain.Order.Order>().Update(order);
    }

    public Task<bool> OrderStatusExistsAsync(int statusId)
    {
        return _context.Set<Domain.Order.OrderStatus>().AnyAsync(s => s.Id == statusId);
    }
    public async Task<string?> GetOrderStatusNameAsync(int statusId)
    {
        var status = await _context.Set<Domain.Order.OrderStatus>().FindAsync(statusId);
        return status?.Name;
    }

    public async Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Set<Domain.Order.Order>().AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var generalStats = await query
            .GroupBy(o => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.FinalAmount),
                AverageOrderValue = g.Average(o => (double)o.FinalAmount)
            })
            .FirstOrDefaultAsync();

        var statusStats = await _context.Set<Domain.Order.Order>()
            .Include(o => o.OrderStatus)
            .Where(o => !fromDate.HasValue || o.CreatedAt >= fromDate.Value)
            .Where(o => !toDate.HasValue || o.CreatedAt <= toDate.Value)
            .GroupBy(o => new { o.OrderStatusId, o.OrderStatus!.Name })
            .Select(g => new
            {
                StatusId = g.Key.OrderStatusId,
                StatusName = g.Key.Name,
                Count = g.Count()
            })
            .ToListAsync();

        return new
        {
            GeneralStatistics = generalStats ?? new { TotalOrders = 0, TotalRevenue = (decimal)0, AverageOrderValue = 0.0 },
            StatusStatistics = statusStats
        };
    }
}