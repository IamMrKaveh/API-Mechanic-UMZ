using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Analytics.Services;

public sealed class AnalyticsQueryService(DBContext context) : IAnalyticsQueryService
{
    private readonly DBContext _context = context;

    private static readonly string[] PaidOrderStatuses =
    [
        OrderStatusValue.Paid.Value,
        OrderStatusValue.Processing.Value,
        OrderStatusValue.Shipped.Value,
        OrderStatusValue.Delivered.Value
    ];

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var ordersQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.CreatedAt >= from && o.CreatedAt <= to);

        var totalOrders = await ordersQuery.CountAsync(ct);

        var statusCounts = await ordersQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var revenueQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.CreatedAt >= from && o.CreatedAt <= to
                && PaidOrderStatuses.Contains(o.Status.Value));

        var revenueData = await revenueQuery
            .Select(o => new { o.FinalAmount, o.TotalProfit })
            .ToListAsync(ct);

        var totalRevenue = revenueData.Sum(o => o.FinalAmount.Amount);
        var totalProfit = revenueData.Sum(o => o.TotalProfit.Amount);
        var paidOrderCount = revenueData.Count;

        var totalUsers = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => !u.IsDeleted, ct);

        var newUsers = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => !u.IsDeleted && u.CreatedAt >= from && u.CreatedAt <= to, ct);

        var totalProducts = await _context.Products
            .IgnoreQueryFilters()
            .CountAsync(p => !p.IsDeleted, ct);

        var activeProducts = await _context.Products
            .IgnoreQueryFilters()
            .CountAsync(p => !p.IsDeleted && p.IsActive, ct);

        var outOfStockVariants = await _context.ProductVariants
            .IgnoreQueryFilters()
            .CountAsync(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited
                && (v.StockQuantity - v.ReservedQuantity) <= 0, ct);

        var lowStockVariants = await _context.ProductVariants
            .IgnoreQueryFilters()
            .CountAsync(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited
                && (v.StockQuantity - v.ReservedQuantity) > 0
                && (v.StockQuantity - v.ReservedQuantity) <= v.LowStockThreshold, ct);

        var cancelledCount = statusCounts
            .Where(s => s.Status.Value == OrderStatusValue.Cancelled.Value)
            .Sum(s => s.Count);

        return new DashboardStatisticsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = statusCounts.Where(s => s.Status.Value == OrderStatusValue.Pending.Value).Sum(s => s.Count),
            ProcessingOrders = statusCounts.Where(s => s.Status.Value == OrderStatusValue.Processing.Value).Sum(s => s.Count),
            ShippedOrders = statusCounts.Where(s => s.Status.Value == OrderStatusValue.Shipped.Value).Sum(s => s.Count),
            DeliveredOrders = statusCounts.Where(s => s.Status.Value == OrderStatusValue.Delivered.Value).Sum(s => s.Count),
            CancelledOrders = cancelledCount,
            TotalRevenue = totalRevenue,
            TotalProfit = totalProfit,
            AverageOrderValue = paidOrderCount > 0 ? Math.Round(totalRevenue / paidOrderCount, 0) : 0,
            TotalUsers = totalUsers,
            NewUsersInPeriod = newUsers,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            OutOfStockVariants = outOfStockVariants,
            LowStockVariants = lowStockVariants,
            CancellationRate = totalOrders > 0 ? Math.Round((decimal)cancelledCount / totalOrders * 100, 2) : 0,
            ProfitMargin = totalRevenue > 0 ? Math.Round(totalProfit / totalRevenue * 100, 2) : 0
        };
    }

    public async Task<IReadOnlyList<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        CancellationToken ct = default)
    {
        var orders = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted
                && PaidOrderStatuses.Contains(o.Status.Value)
                && o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .Select(o => new
            {
                o.CreatedAt,
                FinalAmount = o.FinalAmount,
                TotalProfit = o.TotalProfit,
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            })
            .ToListAsync(ct);

        var grouped = groupBy.ToLowerInvariant() switch
        {
            "week" => orders.GroupBy(o => new
            {
                Year = o.CreatedAt.Year,
                Week = System.Globalization.CultureInfo.InvariantCulture.Calendar
                    .GetWeekOfYear(o.CreatedAt, System.Globalization.CalendarWeekRule.FirstDay, DayOfWeek.Saturday)
            })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Week)
            .Select(g => new SalesChartDataPointDto
            {
                Label = $"{g.Key.Year}-W{g.Key.Week}",
                Date = g.Min(o => o.CreatedAt).Date,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.FinalAmount.Amount),
                Profit = g.Sum(o => o.TotalProfit.Amount),
                ItemsSold = g.Sum(o => o.ItemCount)
            }),

            "month" => orders.GroupBy(o => new { o.CreatedAt.Year, o.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new SalesChartDataPointDto
            {
                Label = $"{g.Key.Year}/{g.Key.Month:D2}",
                Date = new DateTime(g.Key.Year, g.Key.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.FinalAmount.Amount),
                Profit = g.Sum(o => o.TotalProfit.Amount),
                ItemsSold = g.Sum(o => o.ItemCount)
            }),

            _ => orders.GroupBy(o => o.CreatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new SalesChartDataPointDto
            {
                Label = g.Key.ToString("yyyy/MM/dd"),
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.FinalAmount.Amount),
                Profit = g.Sum(o => o.TotalProfit.Amount),
                ItemsSold = g.Sum(o => o.ItemCount)
            })
        };

        return grouped.ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.UtcNow;

        var result = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted
                && PaidOrderStatuses.Contains(o.Status.Value)
                && o.CreatedAt >= from && o.CreatedAt <= to)
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId,
                ProductName = g.Key.ProductName,
                Sku = g.Select(oi => oi.VariantSku).FirstOrDefault(),
                TotalQuantitySold = g.Sum(oi => oi.Quantity),
                TotalRevenue = g.Sum(oi => oi.Amount.Amount),
                TotalProfit = g.Sum(oi => oi.Profit.Amount),
                OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                AverageSellingPrice = g.Average(oi => oi.SellingPriceAtOrder.Amount)
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .ToListAsync(ct);

        return result.AsReadOnly();
    }

    public Task<IReadOnlyList<CategoryPerformanceDto>> GetCategoryPerformanceAsync(DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<RevenueReportDto> GetRevenueReportAsync(DateTime fromDate, DateTime toDate, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}