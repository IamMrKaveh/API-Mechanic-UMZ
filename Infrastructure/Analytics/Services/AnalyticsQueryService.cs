using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Common.Contracts;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Analytics.QueryServices;

public sealed class AnalyticsQueryService(
    DBContext context,
    ISqlConnectionFactory connectionFactory) : IAnalyticsQueryService
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
            .Where(o => o.Status == "Delivered" && o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .SumAsync(o => (decimal?)o.FinalAmount.Amount, ct) ?? 0;

        var totalUsers = await context.Users
            .CountAsync(u => u.CreatedAt >= fromDate && u.CreatedAt <= toDate, ct);

        var totalProducts = await context.Products.CountAsync(ct);

        return new DashboardStatisticsDto
        {
            TotalOrders = totalOrders,
            TotalRevenue = totalRevenue,
            NewUsers = totalUsers,
            TotalProducts = totalProducts,
            FromDate = fromDate,
            ToDate = toDate
        };
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime from,
        DateTime to,
        CancellationToken ct = default)
    {
        var revenueByDay = await context.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to && o.Status == "Delivered")
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new RevenueDataPointDto
            {
                Date = g.Key,
                Revenue = g.Sum(o => o.FinalAmount.Amount),
                OrderCount = g.Count()
            })
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        var totalRevenue = revenueByDay.Sum(d => d.Revenue);
        var totalOrders = revenueByDay.Sum(d => d.OrderCount);

        return new RevenueReportDto
        {
            FromDate = from,
            ToDate = to,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            DataPoints = revenueByDay
        };
    }

    public async Task<PaginatedResult<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var fromDate = from ?? DateTime.UtcNow.AddDays(-30);
        var toDate = to ?? DateTime.UtcNow;

        var data = await context.Orders
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .SelectMany(o => o.OrderItems)
            .GroupBy(oi => oi.CategoryId)
            .Select(g => new CategoryPerformanceDto
            {
                CategoryId = g.Key,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(oi => oi.TotalPrice.Amount)
            })
            .ToListAsync(ct);

        return PaginatedResult<CategoryPerformanceDto>.Create(data, data.Count, 1, data.Count);
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken ct = default)
    {
        var totalVariants = await context.ProductVariants
            .CountAsync(v => !v.IsDeleted, ct);

        var outOfStockCount = await context.ProductVariants
            .CountAsync(v => !v.IsDeleted && !v.IsUnlimited && v.StockQuantity <= 0, ct);

        return new InventoryReportDto
        {
            TotalVariants = totalVariants,
            OutOfStockCount = outOfStockCount,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<PaginatedResult<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime from,
        DateTime to,
        string groupBy,
        CancellationToken ct = default)
    {
        var data = await context.Orders
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .GroupBy(o => o.CreatedAt.Date)
            .Select(g => new SalesChartDataPointDto
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(o => o.FinalAmount.Amount)
            })
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        return PaginatedResult<SalesChartDataPointDto>.Create(data, data.Count, 1, data.Count);
    }
}