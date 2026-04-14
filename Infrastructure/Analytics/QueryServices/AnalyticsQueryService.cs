using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;

namespace Infrastructure.Analytics.QueryServices;

public sealed class AnalyticsQueryService(DBContext context) : IAnalyticsQueryService
{
    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var totalOrders = await context.Orders
            .CountAsync(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate, ct);

        var totalRevenue = await context.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .SumAsync(o => (decimal?)o.FinalAmount.Amount, ct) ?? 0;

        var newUsersInPeriod = await context.Users
            .CountAsync(u => u.CreatedAt >= fromDate && u.CreatedAt <= toDate, ct);

        var totalUsers = await context.Users.CountAsync(ct);
        var totalProducts = await context.Products.CountAsync(ct);

        return new DashboardStatisticsDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            NewUsersInPeriod = newUsersInPeriod,
            TotalUsers = totalUsers,
            TotalProducts = totalProducts
        };
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .AsNoTracking()
            .ToListAsync(ct);

        var grossRevenue = orders.Sum(o => o.FinalAmount.Amount);
        var totalDiscounts = orders.Sum(o => o.DiscountAmount.Amount);
        var totalShipping = orders.Sum(o => o.ShippingCost.Amount);
        var netRevenue = grossRevenue - totalDiscounts;
        var totalOrders = orders.Count;
        var avgOrderValue = totalOrders > 0 ? grossRevenue / totalOrders : 0;

        var byStatus = orders
            .GroupBy(o => o.Status.ToString())
            .Select(g => new RevenueByStatusDto
            {
                Status = g.Key,
                Count = g.Count(),
                Amount = g.Sum(o => o.FinalAmount.Amount)
            })
            .ToList();

        return new RevenueReportDto
        {
            FromDate = from,
            ToDate = to,
            GrossRevenue = grossRevenue,
            TotalDiscounts = totalDiscounts,
            TotalShippingIncome = totalShipping,
            NetRevenue = netRevenue,
            TotalOrders = totalOrders,
            AverageOrderValue = avgOrderValue,
            ByStatus = byStatus
        };
    }

    public async Task<PaginatedResult<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        var data = await context.OrderItems
            .Where(oi => oi.OrderId != null)
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.Sku })
            .Select(g => new TopSellingProductDto
            {
                ProductId = g.Key.ProductId.Value,
                ProductName = g.Key.ProductName,
                Sku = g.Key.Sku,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.UnitPrice.Amount * x.Quantity),
                OrderCount = g.Count()
            })
            .OrderByDescending(x => x.TotalQuantitySold)
            .Take(count)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PaginatedResult<TopSellingProductDto>
        {
            Items = data,
            TotalCount = data.Count,
            Page = 1,
            PageSize = count
        };
    }

    public async Task<PaginatedResult<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var data = await context.OrderItems
            .Join(context.Products,
                oi => oi.ProductId,
                p => p.Id,
                (oi, p) => new { oi, p.CategoryId })
            .Join(context.Categories,
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name.Value,
                    x.oi.Quantity,
                    Revenue = x.oi.UnitPrice.Amount * x.oi.Quantity,
                    x.oi.OrderId
                })
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new CategoryPerformanceDto
            {
                CategoryId = g.Key.CategoryId.Value,
                CategoryName = g.Key.CategoryName,
                TotalQuantitySold = g.Sum(x => x.Quantity),
                TotalRevenue = g.Sum(x => x.Revenue),
                OrderCount = g.Select(x => x.OrderId).Distinct().Count()
            })
            .OrderByDescending(x => x.TotalRevenue)
            .AsNoTracking()
            .ToListAsync(ct);

        return new PaginatedResult<CategoryPerformanceDto>
        {
            Items = data,
            TotalCount = data.Count,
            Page = 1,
            PageSize = data.Count
        };
    }

    public async Task<PaginatedResult<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        CancellationToken ct = default)
    {
        var orders = await context.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .Select(o => new
            {
                o.CreatedAt,
                Revenue = o.FinalAmount.Amount,
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            })
            .AsNoTracking()
            .ToListAsync(ct);

        var grouped = groupBy.ToLower() switch
        {
            "month" => orders.GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1)),
            "week" => orders.GroupBy(o => o.CreatedAt.Date.AddDays(-(int)o.CreatedAt.DayOfWeek)),
            _ => orders.GroupBy(o => o.CreatedAt.Date)
        };

        var data = grouped
            .Select(g => new SalesChartDataPointDto
            {
                Date = g.Key,
                Label = g.Key.ToString("yyyy-MM-dd"),
                OrderCount = g.Count(),
                Revenue = g.Sum(x => x.Revenue),
                ItemsSold = g.Sum(x => x.ItemCount)
            })
            .OrderBy(x => x.Date)
            .ToList();

        return new PaginatedResult<SalesChartDataPointDto>
        {
            Items = data,
            TotalCount = data.Count,
            Page = 1,
            PageSize = data.Count
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken ct = default)
    {
        var inventories = await context.Inventories
            .AsNoTracking()
            .ToListAsync(ct);

        var totalVariants = inventories.Count;
        var inStockVariants = inventories.Count(i => i.AvailableQuantity > 0);
        var outOfStockVariants = inventories.Count(i => i.AvailableQuantity <= 0);

        return new InventoryReportDto
        {
            TotalVariants = totalVariants,
            ActiveVariants = inStockVariants,
            InStockVariants = inStockVariants,
            OutOfStockVariants = outOfStockVariants,
            LowStockVariants = inventories.Count(i => i.AvailableQuantity > 0 && i.AvailableQuantity <= 5)
        };
    }
}