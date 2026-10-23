using System.Globalization;
using Application.Analytics.Contracts;
using Application.Analytics.Features.Shared;
using Application.Common.Validation;

namespace Infrastructure.Analytics.QueryServices;

public sealed class AnalyticsQueryService(DBContext context) : IAnalyticsQueryService
{
    private const int LowStockDefaultThreshold = 5;

    /// <summary>
    /// The longest period a report may cover. A wider range has to be narrowed
    /// by the caller, otherwise the report has to read the whole table.
    /// </summary>
    private const int MaxReportRangeDays = 730;

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
        var (fromDate, toDate) = ClampRange(from, to);

        var totals = await context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                GrossRevenue = g.Sum(o => o.FinalAmount.Amount),
                TotalDiscounts = g.Sum(o => o.DiscountAmount.Amount),
                TotalShipping = g.Sum(o => o.ShippingCost.Amount),
                TotalOrders = g.Count()
            })
            .FirstOrDefaultAsync(ct);

        var byStatus = await context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .GroupBy(o => o.Status)
            .Select(g => new RevenueByStatusDto
            {
                Status = g.Key.ToString(),
                Count = g.Count(),
                Amount = g.Sum(o => o.FinalAmount.Amount)
            })
            .ToListAsync(ct);

        var grossRevenue = totals?.GrossRevenue ?? 0m;
        var totalDiscounts = totals?.TotalDiscounts ?? 0m;
        var totalOrders = totals?.TotalOrders ?? 0;
        var avgOrderValue = totalOrders > 0 ? grossRevenue / totalOrders : 0;

        return new RevenueReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            GrossRevenue = grossRevenue,
            TotalDiscounts = totalDiscounts,
            TotalShippingIncome = totals?.TotalShipping ?? 0m,
            NetRevenue = grossRevenue - totalDiscounts,
            TotalOrders = totalOrders,
            AverageOrderValue = avgOrderValue,
            ByStatus = byStatus
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken ct = default)
    {
        var totalVariants = await context.Inventories.AsNoTracking().CountAsync(ct);

        var inStockVariants = await context.Inventories
            .AsNoTracking()
            .CountAsync(i => i.IsUnlimited || (i.StockQuantity.Value - i.ReservedQuantity.Value) > 0, ct);

        var outOfStockVariants = await context.Inventories
            .AsNoTracking()
            .CountAsync(i => !i.IsUnlimited && (i.StockQuantity.Value - i.ReservedQuantity.Value) <= 0, ct);

        var lowStockVariants = await context.Inventories
            .AsNoTracking()
            .CountAsync(i =>
                !i.IsUnlimited &&
                (i.StockQuantity.Value - i.ReservedQuantity.Value) > 0 &&
                (i.StockQuantity.Value - i.ReservedQuantity.Value) <= LowStockDefaultThreshold,
                ct);

        return new InventoryReportDto
        {
            TotalVariants = totalVariants,
            ActiveVariants = inStockVariants,
            InStockVariants = inStockVariants,
            OutOfStockVariants = outOfStockVariants,
            LowStockVariants = lowStockVariants
        };
    }

    public async Task<PaginatedResult<TopSellingProductDto>> GetTopSellingProductsAsync(
        int count,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (from, to) = ClampRange(fromDate, toDate);
        var size = Math.Clamp(pageSize, 1, PaginationDefaults.MaxPageSize);
        var currentPage = Math.Max(1, page);

        var query = context.OrderItems
            .AsNoTracking()
            .Where(oi => oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to);

        var totalCount = await query
            .GroupBy(oi => new { oi.ProductId, oi.ProductName, oi.Sku })
            .Select(g => g.Key)
            .CountAsync(ct);

        var data = await query
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
            .Skip((currentPage - 1) * size)
            .Take(Math.Min(size, Math.Max(1, count)))
            .ToListAsync(ct);

        return PaginatedResult<TopSellingProductDto>.Create(data, totalCount, currentPage, size);
    }

    public async Task<PaginatedResult<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (fromDate, toDate) = ClampRange(from, to);
        var size = Math.Clamp(pageSize, 1, PaginationDefaults.MaxPageSize);
        var currentPage = Math.Max(1, page);

        var data = await context.OrderItems
            .Join(context.Products,
                oi => oi.ProductId,
                p => p.Id,
                (oi, p) => new { oi, p.Brand })
            // Date filter must match the totalCount query below; without it the
            // data query scans the entire OrderItems table and disagrees with the count.
            .Where(x => x.Brand != null
                        && x.oi.Order.CreatedAt >= fromDate
                        && x.oi.Order.CreatedAt <= toDate)
            .Join(context.Categories,
                x => x.Brand!.CategoryId,
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
            .Skip((currentPage - 1) * size)
            .Take(size)
            .AsNoTracking()
            .ToListAsync(ct);

        var totalCount = await context.OrderItems
            .Join(context.Products,
                oi => oi.ProductId,
                p => p.Id,
                (oi, p) => new { oi, p.Brand })
            .Where(x => x.Brand != null)
            .Join(context.Categories,
                x => x.Brand!.CategoryId,
                c => c.Id,
                (x, c) => new
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name.Value,
                    x.oi.OrderId,
                    x.oi.Order.CreatedAt
                })
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => g.Key)
            .CountAsync(ct);

        return PaginatedResult<CategoryPerformanceDto>.Create(data, totalCount, currentPage, size);
    }

    public async Task<PaginatedResult<SalesChartDataPointDto>> GetSalesChartDataAsync(
        DateTime fromDate,
        DateTime toDate,
        string groupBy,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var (from, to) = ClampRange(fromDate, toDate);
        var size = Math.Clamp(pageSize, 1, PaginationDefaults.MaxPageSize);
        var currentPage = Math.Max(1, page);

        // The bucket is chosen in SQL, so only the requested page of buckets is
        // transferred instead of every order in the range.
        var orders = context.Orders
            .AsNoTracking()
            .Where(o => o.CreatedAt >= from && o.CreatedAt <= to)
            .Select(o => new
            {
                o.CreatedAt,
                Revenue = o.FinalAmount.Amount,
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            });

        var grouped = groupBy.ToLowerInvariant() switch
        {
            "month" => orders.GroupBy(o => new DateTime(o.CreatedAt.Year, o.CreatedAt.Month, 1)),
            "week" => orders.GroupBy(o => o.CreatedAt.Date.AddDays(-(int)o.CreatedAt.DayOfWeek)),
            _ => orders.GroupBy(o => o.CreatedAt.Date)
        };

        var buckets = grouped
            .Select(g => new
            {
                Date = g.Key,
                OrderCount = g.Count(),
                Revenue = g.Sum(x => x.Revenue),
                ItemsSold = g.Sum(x => x.ItemCount)
            })
            .OrderBy(x => x.Date);

        var totalCount = await buckets.CountAsync(ct);

        var data = await buckets
            .Skip((currentPage - 1) * size)
            .Take(size)
            .Select(x => new SalesChartDataPointDto
            {
                Date = x.Date,
                Label = x.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                OrderCount = x.OrderCount,
                Revenue = x.Revenue,
                ItemsSold = x.ItemsSold
            })
            .ToListAsync(ct);

        return PaginatedResult<SalesChartDataPointDto>.Create(data, totalCount, currentPage, size);
    }

    /// <summary>
    /// Narrows a requested period to the widest range a report may cover, and
    /// orders the bounds when the caller passed them the wrong way round.
    /// </summary>
    private static (DateTime From, DateTime To) ClampRange(DateTime? from, DateTime? to)
    {
        var toDate = to ?? DateTime.UtcNow;
        var fromDate = from ?? toDate.AddDays(-30);

        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        var maxRange = TimeSpan.FromDays(MaxReportRangeDays);

        if (toDate - fromDate > maxRange)
            fromDate = toDate - maxRange;

        return (fromDate, toDate);
    }
}
