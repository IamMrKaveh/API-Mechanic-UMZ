namespace Infrastructure.Order.Services;

public class OrderQueryService : IOrderQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;

    public OrderQueryService(
        LedkaContext context,
        IMediaService mediaService
        )
    {
        _context = context;
        _mediaService = mediaService;
    }

    public async Task<OrderDto?> GetOrderDetailsAsync(
        int orderId,
        int userId,
        CancellationToken ct = default
        )
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.Shipping)
            .Where(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted)
            .FirstOrDefaultAsync(ct);

        return order == null ? null : MapToOrderDto(order);
    }

    public async Task<AdminOrderDto?> GetAdminOrderDetailsAsync(
        int orderId,
        CancellationToken ct = default
        )
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.Shipping)
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .FirstOrDefaultAsync(ct);

        if (order == null) return null;

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == order.UserId)
            .FirstOrDefaultAsync(ct);

        var dto = MapToAdminOrderDto(order);
        if (user != null)
        {
            dto = dto with
            {
                User = new UserSummaryDto
                {
                    Id = user.Id,
                    PhoneNumber = user.PhoneNumber,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    IsAdmin = user.IsAdmin
                }
            };
        }
        return dto;
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(
        int userId,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Variant)
            .ThenInclude(v => v!.Product)
            .Include(o => o.Shipping)
            .Where(o => o.UserId == userId && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusValue = OrderStatusValue.FromString(status);
            query = query.Where(o => o.Status.Value == statusValue.Value);
        }

        var totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = orders.Select(MapToOrderDto).ToList();

        return PaginatedResult<OrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(
        int? userId,
        string? status,
        DateTime? fromDate,
        DateTime? toDate,
        bool? isPaid,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
    {
        var query = _context.Orders
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted);

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusValue = OrderStatusValue.FromString(status);
            query = query.Where(o => o.Status.Value == statusValue.Value);
        }

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        if (isPaid.HasValue)
        {
            if (isPaid.Value)
                query = query.Where(o => o.PaymentDate != null);
            else
                query = query.Where(o => o.PaymentDate == null);
        }

        var totalItems = await query.CountAsync(ct);

        var dtos = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber.Value,
                UserId = o.UserId,
                Status = o.Status.Value,
                StatusDisplayName = o.Status.DisplayName,
                TotalAmount = o.TotalAmount.Amount,
                FinalAmount = o.FinalAmount.Amount,
                ShippingCost = o.ShippingCost.Amount,
                DiscountAmount = o.DiscountAmount.Amount,
                PaymentDate = o.PaymentDate,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt,
                IsDeleted = o.IsDeleted,
                User = new UserSummaryDto
                {
                    Id = o.User!.Id,
                    PhoneNumber = o.User.PhoneNumber,
                    FirstName = o.User.FirstName,
                    LastName = o.User.LastName,
                    IsAdmin = o.User.IsAdmin
                },
                OrderItems = o.OrderItems.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    ProductId = oi.ProductId,
                    ProductName = oi.ProductName,
                    VariantId = oi.VariantId,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.SellingPriceAtOrder.Amount,
                    TotalPrice = oi.Amount.Amount
                })
                .ToList()
            })
            .ToListAsync(ct);

        return PaginatedResult<AdminOrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken ct = default
        )
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var stats = await query
            .GroupBy(o => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                PaidOrders = g.Count(o => o.PaymentDate != null),
                PendingOrders = g.Count(o => o.Status.Value == "Pending"),
                CancelledOrders = g.Count(o => o.Status.Value == "Cancelled"),
                ProcessingOrders = g.Count(o => o.Status.Value == "Processing"),
                ShippedOrders = g.Count(o => o.Status.Value == "Shipped"),
                DeliveredOrders = g.Count(o => o.Status.Value == "Delivered"),
                TotalRevenue = g.Where(o => o.PaymentDate != null)
                    .Sum(o => o.FinalAmount.Amount),
                TotalProfit = g.Where(o => o.PaymentDate != null)
                    .Sum(o => o.TotalProfit.Amount)
            })
            .FirstOrDefaultAsync(ct);

        if (stats == null)
            return new OrderStatisticsDto();

        var avgOrderValue = stats.PaidOrders > 0 ? stats.TotalRevenue / stats.PaidOrders : 0;

        return new OrderStatisticsDto
        {
            TotalOrders = stats.TotalOrders,
            PaidOrders = stats.PaidOrders,
            PendingOrders = stats.PendingOrders,
            CancelledOrders = stats.CancelledOrders,
            ProcessingOrders = stats.ProcessingOrders,
            ShippedOrders = stats.ShippedOrders,
            DeliveredOrders = stats.DeliveredOrders,
            TotalRevenue = stats.TotalRevenue,
            TotalProfit = stats.TotalProfit,
            AverageOrderValue = avgOrderValue,
            PaidOrdersPercentage = stats.TotalOrders > 0 ? Math.Round(Convert.ToDecimal(stats.PaidOrders) / stats.TotalOrders * 100, 2) : 0,
            CancellationRate = stats.TotalOrders > 0 ? Math.Round(Convert.ToDecimal(stats.CancelledOrders) / stats.TotalOrders * 100, 2) : 0,
            ProfitMargin = stats.TotalRevenue > 0 ? Math.Round(stats.TotalProfit / stats.TotalRevenue * 100, 2) : 0
        };
    }

    public async Task<bool> HasUserPurchasedProductAsync(
        int userId,
        int productId,
        CancellationToken ct = default
        )
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted && o.PaymentDate != null)
            .SelectMany(o => o.OrderItems)
            .AnyAsync(oi => oi.ProductId == productId, ct);
    }

    public async Task<int> CountByUserIdAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId && !o.IsDeleted, ct);
    }

    public async Task<decimal> GetTotalSpentByUserIdAsync(
        int userId,
        CancellationToken ct = default
        )
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted && o.PaymentDate != null)
            .SumAsync(o => o.FinalAmount.Amount, ct);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(
        int orderId,
        CancellationToken ct = default
        )
    {
        var order = await _context.Orders.AsNoTracking()
            .Include(o => o.OrderItems).ThenInclude(oi => oi.Variant).ThenInclude(v => v!.Product)
            .Include(o => o.Shipping)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        return order == null ? null : MapToOrderDto(order);
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(
        int userId,
        int page,
        int pageSize,
        CancellationToken ct = default
        )
    {
        return await GetUserOrdersAsync(userId, null, page, pageSize, ct);
    }

    private static OrderDto MapToOrderDto(
        Domain.Order.Order order
        )
    {
        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            OrderNumber = order.OrderNumber.Value,
            ReceiverName = order.AddressSnapshot?.ReceiverName ?? string.Empty,
            Status = order.Status.Value,
            StatusDisplayName = order.Status.DisplayName,
            TotalAmount = order.TotalAmount.Amount,
            TotalProfit = order.TotalProfit.Amount,
            ShippingCost = order.ShippingCost.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            FinalAmount = order.FinalAmount.Amount,
            ShippingId = order.ShippingId,
            DiscountCodeId = order.DiscountCodeId,
            PaymentDate = order.PaymentDate,
            ShippedDate = order.ShippedDate,
            DeliveryDate = order.DeliveryDate,
            CancellationReason = order.CancellationReason,
            IsPaid = order.IsPaid,
            IsCancelled = order.IsCancelled,
            RowVersion = order.RowVersion != null ? Convert.ToBase64String(order.RowVersion) : null,
            CreatedAt = order.CreatedAt,
            OrderItems = order.OrderItems.Select(MapToOrderItemDto).ToList()
        };
    }

    private static AdminOrderDto MapToAdminOrderDto(
        Domain.Order.Order order
        )
    {
        var baseDto = MapToOrderDto(order);
        return new AdminOrderDto
        {
            Id = baseDto.Id,
            UserId = baseDto.UserId,
            OrderNumber = baseDto.OrderNumber,
            ReceiverName = baseDto.ReceiverName,
            Status = baseDto.Status,
            StatusDisplayName = baseDto.StatusDisplayName,
            TotalAmount = baseDto.TotalAmount,
            TotalProfit = order.TotalProfit.Amount,
            ShippingCost = baseDto.ShippingCost,
            DiscountAmount = baseDto.DiscountAmount,
            FinalAmount = baseDto.FinalAmount,
            ShippingId = baseDto.ShippingId,
            DiscountCodeId = baseDto.DiscountCodeId,
            PaymentDate = baseDto.PaymentDate,
            ShippedDate = baseDto.ShippedDate,
            DeliveryDate = baseDto.DeliveryDate,
            CancellationReason = baseDto.CancellationReason,
            IsPaid = baseDto.IsPaid,
            IsCancelled = baseDto.IsCancelled,
            RowVersion = baseDto.RowVersion,
            CreatedAt = baseDto.CreatedAt,
            OrderItems = baseDto.OrderItems,
            OrderItemsCount = order.OrderItems.Count
        };
    }

    private static OrderItemDto MapToOrderItemDto(
        OrderItem item
        )
    {
        return new OrderItemDto
        {
            Id = item.Id,
            OrderId = item.OrderId,
            VariantId = item.VariantId,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            VariantSku = item.VariantSku,
            Quantity = item.Quantity,
            PurchasePriceAtOrder = item.PurchasePriceAtOrder.Amount,
            SellingPriceAtOrder = item.SellingPriceAtOrder.Amount,
            OriginalPriceAtOrder = item.OriginalPriceAtOrder.Amount,
            DiscountAtOrder = item.DiscountAtOrder.Amount,
            Amount = item.Amount.Amount,
            Profit = item.Profit.Amount
        };
    }
}