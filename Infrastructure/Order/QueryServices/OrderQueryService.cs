namespace Infrastructure.Order.QueryServices;

public class OrderQueryService : IOrderQueryService
{
    private readonly LedkaContext _context;
    private readonly IMediaService _mediaService;
    private readonly IMapper _mapper;

    public OrderQueryService(LedkaContext context, IMediaService mediaService, IMapper mapper)
    {
        _context = context;
        _mediaService = mediaService;
        _mapper = mapper;
    }

    public async Task<OrderDto?> GetOrderDetailsAsync(int orderId, int userId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
            .Where(o => o.Id == orderId && o.UserId == userId && !o.IsDeleted)
            .FirstOrDefaultAsync(ct);

        if (order == null) return null;

        return await MapToOrderDtoAsync(order, ct);
    }

    public async Task<AdminOrderDto?> GetAdminOrderDetailsAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
            .IgnoreQueryFilters()
            .Where(o => o.Id == orderId)
            .FirstOrDefaultAsync(ct);

        if (order == null) return null;

        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == order.UserId)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .FirstOrDefaultAsync(ct);

        return await MapToAdminOrderDtoAsync(order, user, ct);
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(
        int userId, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
            .Where(o => o.UserId == userId && !o.IsDeleted);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusValue = OrderStatusValue.FromString(status);
            query = query.Where(o => o.Status == statusValue);
        }

        var totalItems = await query.CountAsync(ct);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var dtos = new List<OrderDto>();
        foreach (var order in orders)
        {
            dtos.Add(await MapToOrderDtoAsync(order, ct));
        }

        return PaginatedResult<OrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(
        int? userId, string? status, DateTime? fromDate, DateTime? toDate,
        bool? isPaid, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
            .IgnoreQueryFilters()
            .Where(o => !o.IsDeleted);

        if (userId.HasValue)
            query = query.Where(o => o.UserId == userId.Value);

        if (!string.IsNullOrWhiteSpace(status))
        {
            var statusValue = OrderStatusValue.FromString(status);
            query = query.Where(o => o.Status == statusValue);
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

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var userIds = orders.Select(o => o.UserId).Distinct().ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                PhoneNumber = u.PhoneNumber,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsAdmin = u.IsAdmin
            })
            .ToDictionaryAsync(u => u.Id, ct);

        var dtos = new List<AdminOrderDto>();
        foreach (var order in orders)
        {
            users.TryGetValue(order.UserId, out var user);
            dtos.Add(await MapToAdminOrderDtoAsync(order, user, ct));
        }

        return PaginatedResult<AdminOrderDto>.Create(dtos, totalItems, page, pageSize);
    }

    public async Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => !o.IsDeleted);

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var orders = await query.ToListAsync(ct);

        var totalOrders = orders.Count;
        var paidOrders = orders.Count(o => o.IsPaid);
        var pendingOrders = orders.Count(o => o.IsPending);
        var cancelledOrders = orders.Count(o => o.IsCancelled);
        var processingOrders = orders.Count(o => o.IsProcessing);
        var shippedOrders = orders.Count(o => o.IsShipped);
        var deliveredOrders = orders.Count(o => o.IsDelivered);

        var totalRevenue = orders
            .Where(o => o.IsPaid || o.IsProcessing || o.IsShipped || o.IsDelivered)
            .Sum(o => o.FinalAmount.Amount);

        var totalProfit = orders
            .Where(o => o.IsPaid || o.IsProcessing || o.IsShipped || o.IsDelivered)
            .Sum(o => o.TotalProfit.Amount);

        var avgOrderValue = paidOrders > 0 ? totalRevenue / paidOrders : 0;

        return new OrderStatisticsDto
        {
            TotalOrders = totalOrders,
            PaidOrders = paidOrders,
            PendingOrders = pendingOrders,
            CancelledOrders = cancelledOrders,
            ProcessingOrders = processingOrders,
            ShippedOrders = shippedOrders,
            DeliveredOrders = deliveredOrders,
            TotalRevenue = totalRevenue,
            TotalProfit = totalProfit,
            AverageOrderValue = avgOrderValue,
            PaidOrdersPercentage = totalOrders > 0 ? Math.Round((decimal)paidOrders / totalOrders * 100, 2) : 0,
            CancellationRate = totalOrders > 0 ? Math.Round((decimal)cancelledOrders / totalOrders * 100, 2) : 0,
            ProfitMargin = totalRevenue > 0 ? Math.Round(totalProfit / totalRevenue * 100, 2) : 0
        };
    }

    public async Task<bool> HasUserPurchasedProductAsync(
        int userId, int productId, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted && o.PaymentDate != null)
            .SelectMany(o => o.OrderItems)
            .AnyAsync(oi => oi.ProductId == productId, ct);
    }

    public async Task<int> CountByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .CountAsync(o => o.UserId == userId && !o.IsDeleted, ct);
    }

    public async Task<decimal> GetTotalSpentByUserIdAsync(int userId, CancellationToken ct = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId && !o.IsDeleted && o.PaymentDate != null)
            .SumAsync(o => o.FinalAmount.Amount, ct);
    }

    #region Private Mapping Helpers

    private async Task<OrderDto> MapToOrderDtoAsync(Domain.Order.Order order, CancellationToken ct)
    {
        var itemDtos = new List<OrderItemDto>();
        foreach (var item in order.OrderItems)
        {
            var productIcon = await _mediaService.GetPrimaryImageUrlAsync("Product", item.ProductId);
            itemDtos.Add(new OrderItemDto
            {
                Id = item.Id,
                VariantId = item.VariantId,
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                VariantSku = item.VariantSku,
                VariantAttributes = item.VariantAttributes,
                ProductIcon = productIcon,
                PurchasePriceAtOrder = item.PurchasePriceAtOrder.Amount,
                SellingPriceAtOrder = item.SellingPriceAtOrder.Amount,
                OriginalPriceAtOrder = item.OriginalPriceAtOrder.Amount,
                DiscountAtOrder = item.DiscountAtOrder.Amount,
                Quantity = item.Quantity,
                Amount = item.Amount.Amount,
                Profit = item.Profit.Amount
            });
        }

        ShippingMethodDto? shippingDto = null;
        if (order.ShippingMethod != null)
        {
            shippingDto = new ShippingMethodDto
            {
                Id = order.ShippingMethod.Id,
                Name = order.ShippingMethod.Name,
                Cost = order.ShippingMethod.BaseCost.Amount,
                Description = order.ShippingMethod.Description,
                EstimatedDeliveryTime = order.ShippingMethod.GetDeliveryTimeDisplay(),
                IsActive = order.ShippingMethod.IsActive,
                IsDeleted = order.ShippingMethod.IsDeleted
            };
        }

        return new OrderDto
        {
            Id = order.Id,
            UserId = order.UserId,
            ReceiverName = order.ReceiverName,
            OrderNumber = order.OrderNumber.Value,
            TotalAmount = order.TotalAmount.Amount,
            ShippingCost = order.ShippingCost.Amount,
            DiscountAmount = order.DiscountAmount.Amount,
            FinalAmount = order.FinalAmount.Amount,
            Status = order.Status.Value,
            StatusDisplayName = order.Status.DisplayName,
            CreatedAt = order.CreatedAt,
            PaymentDate = order.PaymentDate,
            ShippedDate = order.ShippedDate,
            DeliveryDate = order.DeliveryDate,
            ShippingMethodId = order.ShippingMethodId,
            ShippingMethod = shippingDto,
            IsPaid = order.IsPaid,
            IsCancelled = order.IsCancelled,
            CancellationReason = order.CancellationReason,
            RowVersion = order.RowVersion != null ? Convert.ToBase64String(order.RowVersion) : null,
            OrderItems = itemDtos
        };
    }

    private async Task<AdminOrderDto> MapToAdminOrderDtoAsync(
        Domain.Order.Order order, UserSummaryDto? user, CancellationToken ct)
    {
        var baseDto = await MapToOrderDtoAsync(order, ct);

        return new AdminOrderDto
        {
            Id = baseDto.Id,
            UserId = baseDto.UserId,
            ReceiverName = baseDto.ReceiverName,
            OrderNumber = baseDto.OrderNumber,
            TotalAmount = baseDto.TotalAmount,
            ShippingCost = baseDto.ShippingCost,
            DiscountAmount = baseDto.DiscountAmount,
            FinalAmount = baseDto.FinalAmount,
            Status = baseDto.Status,
            StatusDisplayName = baseDto.StatusDisplayName,
            CreatedAt = baseDto.CreatedAt,
            PaymentDate = baseDto.PaymentDate,
            ShippedDate = baseDto.ShippedDate,
            DeliveryDate = baseDto.DeliveryDate,
            ShippingMethodId = baseDto.ShippingMethodId,
            ShippingMethod = baseDto.ShippingMethod,
            IsPaid = baseDto.IsPaid,
            IsCancelled = baseDto.IsCancelled,
            CancellationReason = baseDto.CancellationReason,
            RowVersion = baseDto.RowVersion,
            OrderItems = baseDto.OrderItems,
            TotalProfit = order.TotalProfit.Amount,
            User = user,
            OrderItemsCount = order.OrderItems.Count
        };
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _context.Orders.AsNoTracking().Include(o => o.OrderItems).Include(o => o.ShippingMethod)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        return order == null ? null : await MapToOrderDtoAsync(order, ct);
    }

    public async Task<PaginatedResult<OrderDto>> GetUserOrdersAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        return await GetUserOrdersAsync(userId, null, page, pageSize, ct);
    }

    #endregion Private Mapping Helpers
}