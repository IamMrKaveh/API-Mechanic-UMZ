namespace Infrastructure.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private readonly LedkaContext _context;
    private readonly ILogger<AnalyticsService> _logger;
    private readonly ICacheService _cacheService;

    public AnalyticsService(
        LedkaContext context,
        ILogger<AnalyticsService> logger,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<object> GetDashboardStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var cacheKey = $"dashboard_stats_{fromDate?.ToString("yyyyMMdd")}_{toDate?.ToString("yyyyMMdd")}";
        var cached = await _cacheService.GetAsync<object>(cacheKey);
        if (cached != null) return cached;

        var ordersQuery = _context.Orders.Where(o => o.IsPaid);
        if (fromDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) ordersQuery = ordersQuery.Where(o => o.CreatedAt <= toDate.Value);

        var totalOrders = await ordersQuery.CountAsync();
        var totalRevenue = await ordersQuery.SumAsync(o => o.FinalAmount);
        var totalProfit = await ordersQuery.SumAsync(o => o.TotalProfit);
        var averageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0;

        var totalUsers = await _context.Users.CountAsync(u => !u.IsDeleted);
        var newUsersCount = await _context.Users
            .Where(u => !u.IsDeleted && u.CreatedAt >= (fromDate ?? DateTime.UtcNow.AddDays(-30)))
            .CountAsync();

        var totalProducts = await _context.Products.CountAsync(p => !p.IsDeleted && p.IsActive);
        var lowStockCount = await _context.ProductVariants
            .CountAsync(v => !v.IsUnlimited && !v.IsDeleted && v.Stock > 0 && v.Stock <= 5);
        var outOfStockCount = await _context.ProductVariants
            .CountAsync(v => !v.IsUnlimited && !v.IsDeleted && v.Stock == 0);

        var result = new
        {
            Orders = new
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalProfit = totalProfit,
                AverageOrderValue = averageOrderValue
            },
            Users = new
            {
                TotalUsers = totalUsers,
                NewUsers = newUsersCount
            },
            Products = new
            {
                TotalProducts = totalProducts,
                LowStock = lowStockCount,
                OutOfStock = outOfStockCount
            }
        };

        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(15));
        return result;
    }

    public async Task<IEnumerable<object>> GetSalesChartDataAsync(DateTime fromDate, DateTime toDate, string groupBy = "day")
    {
        var orders = await _context.Orders
            .Where(o => o.IsPaid && o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .Select(o => new { o.CreatedAt, o.FinalAmount, o.TotalProfit })
            .ToListAsync();

        var groupedData = groupBy.ToLower() switch
        {
            "month" => orders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
                            .Select(g => new
                            {
                                Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                                Revenue = g.Sum(o => o.FinalAmount),
                                Profit = g.Sum(o => o.TotalProfit),
                                OrderCount = g.Count()
                            }),
            "week" => orders.GroupBy(o => new { o.CreatedAt.Year, Week = System.Globalization.ISOWeek.GetWeekOfYear(o.CreatedAt) })
                           .Select(g => new
                           {
                               Period = $"{g.Key.Year}-W{g.Key.Week:D2}",
                               Revenue = g.Sum(o => o.FinalAmount),
                               Profit = g.Sum(o => o.TotalProfit),
                               OrderCount = g.Count()
                           }),
            _ => orders.GroupBy(o => o.CreatedAt.Date)
                      .Select(g => new
                      {
                          Period = g.Key.ToString("yyyy-MM-dd"),
                          Revenue = g.Sum(o => o.FinalAmount),
                          Profit = g.Sum(o => o.TotalProfit),
                          OrderCount = g.Count()
                      })
        };

        return groupedData.OrderBy(g => g.Period).ToList();
    }

    public async Task<IEnumerable<object>> GetTopSellingProductsAsync(int count = 10, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OrderItems
            .Include(oi => oi.Variant)
            .ThenInclude(v => v.Product)
            .Where(oi => oi.Order.IsPaid);

        if (fromDate.HasValue) query = query.Where(oi => oi.Order.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(oi => oi.Order.CreatedAt <= toDate.Value);

        var topProducts = await query
            .GroupBy(oi => new { oi.Variant.ProductId, oi.Variant.Product.Name })
            .Select(g => new
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.Name,
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Amount),
                TotalProfit = g.Sum(oi => oi.Profit)
            })
            .OrderByDescending(p => p.TotalQuantity)
            .Take(count)
            .ToListAsync();

        return topProducts;
    }

    public async Task<IEnumerable<object>> GetCategoryPerformanceAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _context.OrderItems
            .Include(oi => oi.Variant)
            .ThenInclude(v => v.Product)
            .ThenInclude(p => p.CategoryGroup)
            .ThenInclude(cg => cg.Category)
            .Where(oi => oi.Order.IsPaid);

        if (fromDate.HasValue) query = query.Where(oi => oi.Order.CreatedAt >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(oi => oi.Order.CreatedAt <= toDate.Value);

        var categoryPerformance = await query
            .GroupBy(oi => new
            {
                CategoryId = oi.Variant.Product.CategoryGroup.CategoryId,
                CategoryName = oi.Variant.Product.CategoryGroup.Category.Name
            })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalQuantity = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Amount),
                TotalProfit = g.Sum(oi => oi.Profit),
                OrderCount = g.Select(oi => oi.OrderId).Distinct().Count()
            })
            .OrderByDescending(c => c.TotalRevenue)
            .ToListAsync();

        return categoryPerformance;
    }
}