using Infrastructure.Persistence.Interface.Order;

namespace Infrastructure.Persistence.Repositories.Order;

public class OrderRepository : IOrderRepository
{
    private readonly LedkaContext _context;

    public OrderRepository(LedkaContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Domain.Order.Order> Orders, int TotalItems)> GetOrdersAsync(int? userId, bool isAdmin, int? filterUserId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.Orders.Include(o => o.User).Include(o => o.OrderStatus).Include(o => o.ShippingMethod).Include(o => o.OrderItems).AsQueryable();
        if (!isAdmin && userId.HasValue) query = query.Where(o => o.UserId == userId.Value);
        if (isAdmin && filterUserId.HasValue) query = query.Where(o => o.UserId == filterUserId.Value);
        if (statusId.HasValue) query = query.Where(o => o.OrderStatusId == statusId.Value);
        var totalItems = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (orders, totalItems);
    }

    public async Task<Domain.Order.Order?> GetOrderByIdAsync(int orderId, int? userId, bool isAdmin)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.ShippingMethod)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.CategoryGroup)
                            .ThenInclude(cg => cg.Category)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.AttributeType)
            .AsQueryable();

        if (!isAdmin && userId.HasValue)
        {
            query = query.Where(o => o.UserId == userId.Value);
        }

        return await query.FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Domain.Order.Order?> GetOrderByAuthorityAsync(string authority)
    {
        return await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.PaymentTransactions.Any(pt => pt.Authority == authority));
    }

    public async Task<Domain.Order.Order?> GetOrderForUpdateAsync(int orderId)
    {
        return await _context.Orders
             .FromSqlInterpolated($"SELECT * FROM \"Orders\" WHERE \"Id\" = {orderId} FOR UPDATE")
             .Include(o => o.OrderItems)
             .Include(o => o.DiscountUsages)
             .FirstOrDefaultAsync();
    }

    public async Task<Domain.Order.Order?> GetOrderForPaymentAsync(int orderId)
    {
        return await _context.Orders
             .Include(o => o.OrderItems)
             .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Domain.Order.Order?> GetOrderWithItemsAsync(int orderId)
    {
        return await _context.Orders
           .Include(o => o.DiscountUsages)
           .ThenInclude(d => d.DiscountCode)
           .Include(o => o.OrderItems)
           .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<Domain.Order.Order?> GetOrderByIdempotencyKey(string idempotencyKey, int userId)
    {
        return await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId);
    }

    public async Task<bool> GetExistingPendingOrder(int userId)
    {
        return await _context.Orders
                    .Where(o => o.UserId == userId && o.IsPaid == false && o.CreatedAt > DateTime.UtcNow.AddSeconds(-10))
                    .AnyAsync();
    }

    public async Task<IEnumerable<ProductVariant>> GetVariantsByIdsAsync(IEnumerable<int> variantIds)
    {
        return await _context.ProductVariants
            .Where(v => variantIds.Contains(v.Id))
            .Include(v => v.Product)
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductVariant>> GetVariantsByIdsForUpdateAsync(IEnumerable<int> variantIds)
    {
        if (!variantIds.Any()) return new List<ProductVariant>();
        var idsString = string.Join(",", variantIds);
#pragma warning disable EF1002
        return await _context.ProductVariants
            .FromSqlRaw($"SELECT * FROM \"ProductVariants\" WHERE \"Id\" IN ({idsString}) FOR UPDATE")
            .Include(v => v.Product)
            .Include(v => v.InventoryTransactions)
            .ToListAsync();
#pragma warning restore EF1002
    }

    public async Task<ShippingMethod?> GetShippingMethodByIdAsync(int shippingMethodId)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(s => s.Id == shippingMethodId && !s.IsDeleted);
    }

    public async Task<ShippingMethod?> GetShippingMethodAsync(int shippingMethodId)
    {
        return await _context.ShippingMethods
            .FirstOrDefaultAsync(s => s.Id == shippingMethodId && !s.IsDeleted);
    }

    public async Task AddAsync(Domain.Order.Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task AddOrderAsync(Domain.Order.Order order)
    {
        await _context.Orders.AddAsync(order);
    }

    public async Task AddDiscountUsageAsync(DiscountUsage discountUsage)
    {
        await _context.DiscountUsages.AddAsync(discountUsage);
    }

    public async Task AddPaymentTransactionAsync(PaymentTransaction paymentTransaction)
    {
        await _context.PaymentTransactions.AddAsync(paymentTransaction);
    }

    public async Task<PaymentTransaction?> GetPaymentTransactionAsync(string authority)
    {
        return await _context.PaymentTransactions.FirstOrDefaultAsync(pt => pt.Authority == authority);
    }

    public async Task<PaymentTransaction?> GetPaymentTransactionForUpdateAsync(string authority)
    {
        return await _context.PaymentTransactions
            .FromSqlInterpolated($"SELECT * FROM \"PaymentTransactions\" WHERE \"Authority\" = {authority} FOR UPDATE")
            .FirstOrDefaultAsync();
    }

    public void Update(Domain.Order.Order order)
    {
        _context.Orders.Update(order);
    }

    public void SetOriginalRowVersion(Domain.Order.Order order, byte[] rowVersion)
    {
        _context.Entry(order).Property(o => o.RowVersion).OriginalValue = rowVersion;
    }

    public async Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Orders.Where(o => o.IsPaid);

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        var totalOrders = await query.CountAsync();
        var totalRevenue = await query.SumAsync(o => o.FinalAmount);
        var totalProfit = await query.SumAsync(o => o.TotalProfit);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        return new
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            TotalProfit = totalProfit,
            AverageOrderValue = averageOrderValue
        };
    }

    public async Task<IEnumerable<object>> GetOrderStatusStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Orders.AsQueryable();

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= toDate.Value);
        }

        var statusStats = await query
            .GroupBy(o => new { o.OrderStatusId, o.OrderStatus!.Name })
            .Select(g => new
            {
                StatusId = g.Key.OrderStatusId,
                StatusName = g.Key.Name,
                Count = g.Count(),
                TotalAmount = g.Sum(o => o.FinalAmount)
            })
            .ToListAsync();

        return statusStats;
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(string idempotencyKey)
    {
        return await _context.Orders.AnyAsync(o => o.IdempotencyKey == idempotencyKey);
    }

    public async Task<List<ProductVariant>> GetVariantsWithShippingMethodsAsync(List<int> variantIds)
    {
        return await _context.ProductVariants
            .AsNoTracking()
            .Where(v => variantIds
                .Contains(v.Id) && !v.IsDeleted && v.IsActive)
            .Include(v => v.Product)
            .Include(v => v.ProductVariantShippingMethods
                .Where(pvsm => pvsm.IsActive))
            .ThenInclude(pvsm => pvsm.ShippingMethod)
            .ToListAsync();
    }

    public async Task<List<ShippingMethod>> GetShippingMethodsByIdsAsync(List<int> shippingMethodIds)
    {
        return await _context.ShippingMethods
            .AsNoTracking()
            .Where(sm => shippingMethodIds
                .Contains(sm.Id) && sm.IsActive && !sm.IsDeleted)
            .ToListAsync();
    }
}