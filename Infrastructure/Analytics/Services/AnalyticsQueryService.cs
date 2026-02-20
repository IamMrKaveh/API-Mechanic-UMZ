namespace Infrastructure.Analytics.Services;

/// <summary>
/// Read-side analytics query service.
/// Queries the database directly for reporting (Fast Path - no domain model loading).
/// </summary>
public sealed class AnalyticsQueryService : IAnalyticsQueryService
{
    private readonly LedkaContext _context;
    private readonly ILogger<AnalyticsQueryService> _logger;

    public AnalyticsQueryService(LedkaContext context, ILogger<AnalyticsQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardStatisticsDto> GetDashboardStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
        var to = toDate ?? DateTime.UtcNow;

        // Orders query (ignoring soft-delete filter via status-based logic)
        var ordersQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.CreatedAt >= from && o.CreatedAt <= to);

        var totalOrders = await ordersQuery.CountAsync(cancellationToken);

        var statusCounts = await ordersQuery
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        // Paid/Delivered orders for revenue
        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };
        var revenueQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.CreatedAt >= from && o.CreatedAt <= to
                && paidStatuses.Contains(o.Status));

        var revenueData = await revenueQuery
            .Select(o => new { o.FinalAmount, o.TotalProfit })
            .ToListAsync(cancellationToken);

        var totalRevenue = revenueData.Sum(o => o.FinalAmount.Amount);
        var totalProfit = revenueData.Sum(o => o.TotalProfit.Amount);
        var paidOrderCount = revenueData.Count;

        // Users
        var totalUsers = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => !u.IsDeleted, cancellationToken);

        var newUsers = await _context.Users
            .IgnoreQueryFilters()
            .CountAsync(u => !u.IsDeleted && u.CreatedAt >= from && u.CreatedAt <= to, cancellationToken);

        // Products
        var totalProducts = await _context.Products
            .IgnoreQueryFilters()
            .CountAsync(p => !p.IsDeleted, cancellationToken);

        var activeProducts = await _context.Products
            .IgnoreQueryFilters()
            .CountAsync(p => !p.IsDeleted && p.IsActive, cancellationToken);

        // Stock
        var outOfStockVariants = await _context.ProductVariants
            .IgnoreQueryFilters()
            .CountAsync(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited
                && (v.StockQuantity - v.ReservedQuantity) <= 0, cancellationToken);

        var lowStockVariants = await _context.ProductVariants
            .IgnoreQueryFilters()
            .CountAsync(v => !v.IsDeleted && v.IsActive && !v.IsUnlimited
                && (v.StockQuantity - v.ReservedQuantity) > 0
                && (v.StockQuantity - v.ReservedQuantity) <= v.LowStockThreshold, cancellationToken);

        var cancelledCount = statusCounts
            .Where(s => s.Status == Domain.Order.ValueObjects.OrderStatusValue.Cancelled)
            .Sum(s => s.Count);

        return new DashboardStatisticsDto
        {
            TotalOrders = totalOrders,
            PendingOrders = statusCounts.Where(s => s.Status == Domain.Order.ValueObjects.OrderStatusValue.Pending).Sum(s => s.Count),
            ProcessingOrders = statusCounts.Where(s => s.Status == Domain.Order.ValueObjects.OrderStatusValue.Processing).Sum(s => s.Count),
            ShippedOrders = statusCounts.Where(s => s.Status == Domain.Order.ValueObjects.OrderStatusValue.Shipped).Sum(s => s.Count),
            DeliveredOrders = statusCounts.Where(s => s.Status == Domain.Order.ValueObjects.OrderStatusValue.Delivered).Sum(s => s.Count),
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
        DateTime fromDate, DateTime toDate, string groupBy, CancellationToken cancellationToken = default)
    {
        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };

        var orders = await _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted
                && paidStatuses.Contains(o.Status)
                && o.CreatedAt >= fromDate && o.CreatedAt <= toDate)
            .Select(o => new
            {
                o.CreatedAt,
                FinalAmount = o.FinalAmount,
                TotalProfit = o.TotalProfit,
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            })
            .ToListAsync(cancellationToken);

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

            _ => orders.GroupBy(o => o.CreatedAt.Date) // day
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
        int count, DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.UtcNow;

        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };

        var result = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order != null
                && !oi.Order.IsDeleted
                && paidStatuses.Contains(oi.Order.Status)
                && oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
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
            .ToListAsync(cancellationToken);

        return result.AsReadOnly();
    }

    public async Task<IReadOnlyList<CategoryPerformanceDto>> GetCategoryPerformanceAsync(
        DateTime? fromDate, DateTime? toDate, CancellationToken cancellationToken = default)
    {
        var from = fromDate ?? DateTime.MinValue;
        var to = toDate ?? DateTime.UtcNow;

        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };

        // Get category data through product -> Brand -> category chain
        var salesData = await _context.OrderItems
            .Include(oi => oi.Order)
            .Where(oi => oi.Order != null
                && !oi.Order.IsDeleted
                && paidStatuses.Contains(oi.Order.Status)
                && oi.Order.CreatedAt >= from && oi.Order.CreatedAt <= to)
            .Join(_context.Products.IgnoreQueryFilters().Where(p => !p.IsDeleted),
                oi => oi.ProductId,
                p => p.Id,
                (oi, p) => new { oi, p.BrandId })
            .Join(_context.Brands.IgnoreQueryFilters().Where(cg => !cg.IsDeleted),
                x => x.BrandId,
                cg => cg.Id,
                (x, cg) => new { x.oi, cg.CategoryId })
            .Join(_context.Categories.IgnoreQueryFilters().Where(c => !c.IsDeleted),
                x => x.CategoryId,
                c => c.Id,
                (x, c) => new { x.oi, CategoryId = c.Id, CategoryName = c.Name })
            .GroupBy(x => new { x.CategoryId, x.CategoryName })
            .Select(g => new
            {
                g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                TotalQuantitySold = g.Sum(x => x.oi.Quantity),
                TotalRevenue = g.Sum(x => x.oi.Amount.Amount),
                TotalProfit = g.Sum(x => x.oi.Profit.Amount),
                OrderCount = g.Select(x => x.oi.OrderId).Distinct().Count()
            })
            .ToListAsync(cancellationToken);

        // Category metadata
        var categoryInfo = await _context.Categories
            .IgnoreQueryFilters()
            .Where(c => !c.IsDeleted)
            .Select(c => new
            {
                c.Id,
                Name = c.Name,
                TotalGroups = c.Brands.Count(cg => !cg.IsDeleted),
                TotalProducts = c.Brands
                    .Where(cg => !cg.IsDeleted)
                    .SelectMany(cg => cg.Products)
                    .Count(p => !p.IsDeleted)
            })
            .ToListAsync(cancellationToken);

        var grandTotalRevenue = salesData.Sum(s => s.TotalRevenue);

        var result = categoryInfo.Select(cat =>
        {
            var sales = salesData.FirstOrDefault(s => s.CategoryId == cat.Id);
            var revenue = sales?.TotalRevenue ?? 0;

            return new CategoryPerformanceDto
            {
                CategoryId = cat.Id,
                CategoryName = cat.Name,
                TotalGroups = cat.TotalGroups,
                TotalProducts = cat.TotalProducts,
                TotalQuantitySold = sales?.TotalQuantitySold ?? 0,
                TotalRevenue = revenue,
                TotalProfit = sales?.TotalProfit ?? 0,
                RevenuePercentage = grandTotalRevenue > 0
                    ? Math.Round(revenue / grandTotalRevenue * 100, 2) : 0,
                OrderCount = sales?.OrderCount ?? 0
            };
        })
        .OrderByDescending(x => x.TotalRevenue)
        .ToList();

        return result.AsReadOnly();
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(
        DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var ordersQuery = _context.Orders
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted && o.CreatedAt >= fromDate && o.CreatedAt <= toDate);

        var allOrders = await ordersQuery
            .Select(o => new
            {
                Status = o.Status,
                FinalAmount = o.FinalAmount,
                TotalAmount = o.TotalAmount,
                TotalProfit = o.TotalProfit,
                ShippingCost = o.ShippingCost,
                DiscountAmount = o.DiscountAmount,
                ItemCount = o.OrderItems.Sum(i => i.Quantity)
            })
            .ToListAsync(cancellationToken);

        var paidStatuses = new[] { "Paid", "Processing", "Shipped", "Delivered" };
        var paidOrders = allOrders.Where(o => paidStatuses.Contains(o.Status.Value)).ToList();

        var grossRevenue = paidOrders.Sum(o => o.TotalAmount.Amount);
        var totalDiscounts = paidOrders.Sum(o => o.DiscountAmount.Amount);
        var totalShipping = paidOrders.Sum(o => o.ShippingCost.Amount);
        var netRevenue = paidOrders.Sum(o => o.FinalAmount.Amount);
        var totalProfit = paidOrders.Sum(o => o.TotalProfit.Amount);
        var totalCost = grossRevenue - totalProfit;
        var totalItems = paidOrders.Sum(o => o.ItemCount);

        var byStatus = allOrders
            .GroupBy(o => o.Status.Value)
            .Select(g => new RevenueByStatusDto
            {
                Status = g.Key,
                Count = g.Count(),
                Amount = g.Sum(o => o.FinalAmount.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        return new RevenueReportDto
        {
            FromDate = fromDate,
            ToDate = toDate,
            GrossRevenue = grossRevenue,
            TotalDiscounts = totalDiscounts,
            TotalShippingIncome = totalShipping,
            NetRevenue = netRevenue,
            TotalCost = totalCost,
            GrossProfit = totalProfit,
            ProfitMargin = netRevenue > 0 ? Math.Round(totalProfit / netRevenue * 100, 2) : 0,
            TotalOrders = paidOrders.Count,
            TotalItemsSold = totalItems,
            AverageOrderValue = paidOrders.Count > 0 ? Math.Round(netRevenue / paidOrders.Count, 0) : 0,
            ByStatus = byStatus.AsReadOnly()
        };
    }

    public async Task<InventoryReportDto> GetInventoryReportAsync(CancellationToken cancellationToken = default)
    {
        var variants = await _context.ProductVariants
            .IgnoreQueryFilters()
            .Include(v => v.Product)
            .Where(v => !v.IsDeleted)
            .Select(v => new
            {
                v.Id,
                v.ProductId,
                ProductName = v.Product != null ? v.Product.Name : "",
                v.Sku,
                v.StockQuantity,
                v.ReservedQuantity,
                v.IsUnlimited,
                v.IsActive,
                v.LowStockThreshold,
                v.PurchasePrice,
                v.SellingPrice,
                v.UpdatedAt,
                BrandId = v.Product != null ? v.Product.BrandId : 0
            })
            .ToListAsync(cancellationToken);

        var totalVariants = variants.Count;
        var activeVariants = variants.Count(v => v.IsActive);
        var unlimitedVariants = variants.Count(v => v.IsUnlimited);

        var finiteStockVariants = variants.Where(v => !v.IsUnlimited).ToList();

        var outOfStock = finiteStockVariants
            .Where(v => (v.StockQuantity - v.ReservedQuantity) <= 0)
            .ToList();

        var lowStock = finiteStockVariants
            .Where(v =>
            {
                var available = v.StockQuantity - v.ReservedQuantity;
                return available > 0 && available <= v.LowStockThreshold;
            })
            .ToList();

        var inStockCount = finiteStockVariants.Count(v => (v.StockQuantity - v.ReservedQuantity) > 0)
            + unlimitedVariants;

        var totalStockValue = finiteStockVariants
            .Where(v => (v.StockQuantity - v.ReservedQuantity) > 0)
            .Sum(v => v.PurchasePrice * (v.StockQuantity - v.ReservedQuantity));

        var totalRetailValue = finiteStockVariants
            .Where(v => (v.StockQuantity - v.ReservedQuantity) > 0)
            .Sum(v => v.SellingPrice * (v.StockQuantity - v.ReservedQuantity));

        // Category breakdown
        var BrandIds = variants.Select(v => v.BrandId).Distinct().ToList();
        var categoryMapping = await _context.Brands
            .IgnoreQueryFilters()
            .Where(cg => !cg.IsDeleted && BrandIds.Contains(cg.Id))
            .Join(_context.Categories.IgnoreQueryFilters().Where(c => !c.IsDeleted),
                cg => cg.CategoryId, c => c.Id,
                (cg, c) => new { cg.Id, CategoryId = c.Id, CategoryName = c.Name })
            .ToListAsync(cancellationToken);

        var categoryLookup = categoryMapping.ToDictionary(x => x.Id, x => (x.CategoryId, x.CategoryName));

        var byCategoryData = variants
            .Where(v => !v.IsUnlimited && categoryLookup.ContainsKey(v.BrandId))
            .GroupBy(v => categoryLookup[v.BrandId])
            .Select(g => new InventoryCategoryBreakdownDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName,
                VariantCount = g.Count(),
                TotalStock = g.Sum(v => Math.Max(0, v.StockQuantity - v.ReservedQuantity)),
                StockValue = g.Sum(v => v.PurchasePrice * Math.Max(0, v.StockQuantity - v.ReservedQuantity))
            })
            .OrderByDescending(x => x.StockValue)
            .ToList();

        var lowStockItems = lowStock.Select(x => new AnalyticsLowStockItemDto
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Sku = x.Sku,
            LowStockThreshold = x.LowStockThreshold,
        }).ToList();

        var outOfStockItems = outOfStock.Select(x => new AnalyticsOutOfStockItemDto
        {
            ProductId = x.ProductId,
            ProductName = x.ProductName,
            Sku = x.Sku,
            LastStockDate = x.UpdatedAt
        }).ToList();

        return new InventoryReportDto
        {
            TotalVariants = totalVariants,
            ActiveVariants = activeVariants,
            InStockVariants = inStockCount,
            OutOfStockVariants = outOfStock.Count,
            LowStockVariants = lowStock.Count,
            UnlimitedVariants = unlimitedVariants,
            TotalStockValue = Math.Round(totalStockValue, 0),
            TotalRetailValue = Math.Round(totalRetailValue, 0),
            PotentialProfit = Math.Round(totalRetailValue - totalStockValue, 0),
            ByCategory = byCategoryData.AsReadOnly(),
            LowStockItems = lowStockItems.AsReadOnly(),
            OutOfStockItems = outOfStockItems.AsReadOnly(),
        };
    }
}