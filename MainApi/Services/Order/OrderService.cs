using MainApi.Services.Cache;

namespace MainApi.Services.Order;

public class OrderService : IOrderService
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IZarinpalService _zarinpalService;
    private readonly IConfiguration _configuration;
    private readonly string _baseUrl;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;

    public OrderService(
        MechanicContext context,
        ILogger<OrderService> logger,
        IRateLimitService rateLimitService,
        IHtmlSanitizer htmlSanitizer,
        IZarinpalService zarinpalService,
        IConfiguration configuration,
        IAuditService auditService,
        ICacheService cacheService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _htmlSanitizer = htmlSanitizer;
        _zarinpalService = zarinpalService;
        _configuration = configuration;
        _baseUrl = configuration["LiaraStorage:BaseUrl"] ?? "https://storage.c2.liara.space/mechanic-umz";
        _auditService = auditService;
        _cacheService = cacheService;
    }

    private static string? ConvertToAbsoluteUrl(string? relativeUrl, string baseUrl)
    {
        if (string.IsNullOrEmpty(relativeUrl))
            return null;
        if (Uri.IsWellFormedUriString(relativeUrl, UriKind.Absolute))
            return relativeUrl;

        var cleanRelative = relativeUrl.TrimStart('~', '/');
        return $"{baseUrl}/{cleanRelative}";
    }

    public async Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
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
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Address,
                o.PostalCode,
                o.TotalAmount,
                TotalProfit = isAdmin ? (int?)o.TotalProfit : null,
                o.CreatedAt,
                User = new
                {
                    o.User!.Id,
                    o.User.PhoneNumber,
                    o.User.FirstName,
                    o.User.LastName
                },
                OrderStatus = new
                {
                    o.OrderStatus!.Id,
                    o.OrderStatus.Name,
                    o.OrderStatus.Icon
                },
                OrderItemsCount = o.OrderItems.Count
            })
            .ToListAsync();

        return (orders, totalItems);
    }

    public async Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin)
    {
        var query = _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                    .ThenInclude(p => p!.Category)
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        var baseUrl = _baseUrl;
        var order = await query.Select(o => new
        {
            o.Id,
            o.UserId,
            o.Name,
            o.Address,
            o.PostalCode,
            o.TotalAmount,
            TotalProfit = isAdmin ? (int?)o.TotalProfit : null,
            o.CreatedAt,
            o.OrderStatusId,
            o.RowVersion,
            User = o.User != null ? new
            {
                o.User.Id,
                o.User.PhoneNumber,
                o.User.FirstName,
                o.User.LastName,
                o.User.IsAdmin
            } : null,
            OrderStatus = o.OrderStatus != null ? new
            {
                o.OrderStatus.Id,
                o.OrderStatus.Name,
                o.OrderStatus.Icon
            } : null,
            OrderItems = o.OrderItems.Select(oi => new
            {
                oi.Id,
                PurchasePrice = isAdmin ? (decimal?)oi.PurchasePrice : null,
                oi.SellingPrice,
                oi.Quantity,
                Amount = oi.Amount,
                Profit = isAdmin ? (decimal?)oi.Profit : null,
                ProductIcon = oi.Product != null ? oi.Product.Icon : null,
                ProductId = oi.Product != null ? oi.Product.Id : 0,
                ProductName = oi.Product != null ? oi.Product.Name : null,
                CategoryId = oi.Product != null && oi.Product.Category != null ? oi.Product.Category.Id : 0,
                CategoryName = oi.Product != null && oi.Product.Category != null ? oi.Product.Category.Name : null
            })
        })
        .FirstOrDefaultAsync();

        if (order == null)
        {
            return null;
        }

        if (!isAdmin && order.UserId != currentUserId)
        {
            return null;
        }

        var result = new
        {
            order.Id,
            order.UserId,
            order.Name,
            order.Address,
            order.PostalCode,
            order.TotalAmount,
            order.TotalProfit,
            order.CreatedAt,
            order.OrderStatusId,
            order.RowVersion,
            order.User,
            order.OrderStatus,
            OrderItems = order.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.ProductId,
                oi.PurchasePrice,
                oi.SellingPrice,
                oi.Quantity,
                oi.Amount,
                oi.Profit,
                Product = oi.ProductId > 0 ? new
                {
                    Id = oi.ProductId,
                    Name = oi.ProductName,
                    Icon = ConvertToAbsoluteUrl(oi.ProductIcon, baseUrl),
                    Category = oi.CategoryId > 0 ? new
                    {
                        Id = oi.CategoryId,
                        Name = oi.CategoryName
                    } : null
                } : null
            })
        };

        return result;
    }

    public async Task<TOrders> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        TOrders? order = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var productIds = orderDto.OrderItems.Select(i => i.ProductId).ToList();
                var products = await _context.TProducts
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p);

                foreach (var itemDto in orderDto.OrderItems)
                {
                    if (!products.TryGetValue(itemDto.ProductId, out var product))
                        throw new ArgumentException($"Product {itemDto.ProductId} not found");

                    if (itemDto.SellingPrice < product.PurchasePrice)
                        throw new ArgumentException($"Selling price for product {product.Name} cannot be less than its purchase price.");

                    if (!product.IsUnlimited)
                    {
                        if (product.Count < itemDto.Quantity)
                            throw new InvalidOperationException($"Product {itemDto.ProductId} insufficient stock. Available: {product.Count}, Requested: {itemDto.Quantity}");
                        product.Count -= itemDto.Quantity;
                    }
                }

                order = new TOrders
                {
                    UserId = orderDto.UserId,
                    Name = _htmlSanitizer.Sanitize(orderDto.Name ?? string.Empty),
                    Address = _htmlSanitizer.Sanitize(orderDto.Address ?? string.Empty),
                    PostalCode = _htmlSanitizer.Sanitize(orderDto.PostalCode ?? string.Empty),
                    CreatedAt = DateTime.UtcNow,
                    OrderStatusId = orderDto.OrderStatusId,
                    IdempotencyKey = idempotencyKey
                };

                _context.TOrders.Add(order);
                await _context.SaveChangesAsync();

                decimal totalAmount = 0;
                decimal totalProfit = 0;

                var orderItems = orderDto.OrderItems.Select(itemDto =>
                {
                    var product = products[itemDto.ProductId];
                    var amount = itemDto.SellingPrice * itemDto.Quantity;
                    var profit = (itemDto.SellingPrice - product.PurchasePrice) * itemDto.Quantity;

                    totalAmount += amount;
                    totalProfit += profit;

                    return new TOrderItems
                    {
                        UserOrderId = order.Id,
                        ProductId = itemDto.ProductId,
                        PurchasePrice = product.PurchasePrice,
                        SellingPrice = itemDto.SellingPrice,
                        Quantity = itemDto.Quantity,
                        Amount = amount,
                        Profit = profit
                    };
                }).ToList();

                order.TotalAmount = (int)totalAmount;
                order.TotalProfit = (int)totalProfit;

                _context.TOrderItems.AddRange(orderItems);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return order!;
    }

    public async Task<TOrders> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey)
    {
        var rateLimitKey = $"checkout_{userId}";
        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 1))
        {
            _logger.LogWarning("Rate limit exceeded for checkout by user {UserId}", userId);
            throw new Exception("Too many checkout attempts. Please try again in a minute.");
        }

        var existingOrderByIdempotency = await _context.TOrders.AsNoTracking().FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey && o.UserId == userId);
        if (existingOrderByIdempotency != null)
        {
            _logger.LogInformation("Idempotent checkout request detected for key {IdempotencyKey}, returning existing order {OrderId}", idempotencyKey, existingOrderByIdempotency.Id);
            return existingOrderByIdempotency;
        }

        TOrders? order = null;
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var cart = await _context.TCarts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    throw new InvalidOperationException("Cart is empty");

                const int processingStatusId = 1;

                order = new TOrders
                {
                    UserId = userId,
                    Name = _htmlSanitizer.Sanitize(orderDto.Name ?? string.Empty),
                    Address = _htmlSanitizer.Sanitize(orderDto.Address ?? string.Empty),
                    PostalCode = _htmlSanitizer.Sanitize(orderDto.PostalCode ?? string.Empty),
                    CreatedAt = DateTime.UtcNow,
                    OrderStatusId = processingStatusId,
                    IdempotencyKey = idempotencyKey,
                };
                _context.TOrders.Add(order);
                await _context.SaveChangesAsync();

                var productIds = cart.CartItems.Select(ci => ci.ProductId).ToList();
                var products = await _context.TProducts.Where(p => productIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

                foreach (var item in cart.CartItems)
                {
                    if (!products.TryGetValue(item.ProductId, out var product))
                        throw new InvalidOperationException($"Product with ID '{item.ProductId}' not found.");

                    if (!product.IsUnlimited)
                    {
                        if (product.Count < item.Quantity)
                            throw new DbUpdateConcurrencyException($"Product '{product.Name}' is out of stock or has insufficient quantity.");
                        product.Count -= item.Quantity;
                    }

                    var amount = product.SellingPrice * item.Quantity;
                    var profit = (product.SellingPrice - product.PurchasePrice) * item.Quantity;

                    order.OrderItems.Add(new TOrderItems
                    {
                        ProductId = item.ProductId,
                        PurchasePrice = product.PurchasePrice,
                        SellingPrice = product.SellingPrice,
                        Quantity = item.Quantity,
                        Amount = amount,
                        Profit = profit
                    });
                }

                order.TotalAmount = (int)order.OrderItems.Sum(oi => oi.Amount);
                order.TotalProfit = (int)order.OrderItems.Sum(oi => oi.Profit);

                _context.TOrders.Update(order);
                _context.TCartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogOrderEventAsync(order.Id, "CheckoutFromCart", userId, $"Order created with total amount {order.TotalAmount}");
                await _cacheService.ClearAsync($"cart:user:{userId}");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });

        return order!;
    }

    public async Task<bool> VerifyPaymentAsync(int orderId, string authority)
    {
        var order = await _context.TOrders.FindAsync(orderId);
        if (order == null || order.IsPaid == true) return false;

        var verificationResponse = await _zarinpalService.VerifyPaymentAsync(order.TotalAmount, authority);

        if (verificationResponse != null && verificationResponse.Status == 100)
        {
            order.IsPaid = true;
            order.PaymentRefId = verificationResponse.RefID;
            order.PaymentAuthority = authority;
            order.OrderStatusId = 2; // "در حال پردازش"
            await _context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public string GetFrontendUrl()
    {
        return _configuration["FrontendUrls:BaseUrl"] ?? "http://localhost:4200";
    }

    public async Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto)
    {
        var order = await _context.TOrders.FindAsync(orderId);
        if (order == null) return false;

        if (orderDto.RowVersion != null)
            _context.Entry(order).Property("RowVersion").OriginalValue = orderDto.RowVersion;
        else
            throw new ArgumentException("RowVersion is required for concurrency control.");

        if (orderDto.OrderStatusId.HasValue)
        {
            if (!await _context.TOrderStatus.AnyAsync(s => s.Id == orderDto.OrderStatusId.Value))
                throw new ArgumentException("Invalid order status ID");
            order.OrderStatusId = orderDto.OrderStatusId.Value;
        }

        if (!string.IsNullOrWhiteSpace(orderDto.Name))
            order.Name = _htmlSanitizer.Sanitize(orderDto.Name);
        if (!string.IsNullOrWhiteSpace(orderDto.Address))
            order.Address = _htmlSanitizer.Sanitize(orderDto.Address);
        if (!string.IsNullOrWhiteSpace(orderDto.PostalCode))
            order.PostalCode = _htmlSanitizer.Sanitize(orderDto.PostalCode);

        if (orderDto.DeliveryDate.HasValue)
            order.DeliveryDate = orderDto.DeliveryDate;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var result = false;

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var order = await _context.TOrders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    result = false;
                    return;
                }

                const int shippedStatusId = 3;
                if (order.OrderStatusId >= shippedStatusId)
                    throw new InvalidOperationException("Cannot delete an order that has been shipped or delivered. Consider changing its status instead.");

                foreach (var orderItem in order.OrderItems)
                {
                    if (orderItem.Product != null && !orderItem.Product.IsUnlimited)
                    {
                        orderItem.Product.Count += orderItem.Quantity;
                    }
                }

                _context.TOrderItems.RemoveRange(order.OrderItems);
                _context.TOrders.Remove(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                result = true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
        return result;
    }

    public async Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.TOrders.AsQueryable();

        if (fromDate.HasValue)
            query = query.Where(o => o.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.CreatedAt <= toDate.Value);

        var statistics = await query
            .GroupBy(o => 1)
            .Select(g => new
            {
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(o => o.TotalAmount),
                TotalProfit = g.Sum(o => o.TotalProfit),
                AverageOrderValue = g.Average(o => (double)o.TotalAmount)
            })
            .FirstOrDefaultAsync();

        var statusStatistics = await _context.TOrders
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
            GeneralStatistics = statistics ?? new { TotalOrders = 0, TotalRevenue = 0, TotalProfit = 0, AverageOrderValue = 0.0 },
            StatusStatistics = statusStatistics
        };
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        var order = await _context.TOrders.FindAsync(id);
        if (order == null) return false;

        if (!await _context.TOrderStatus.AnyAsync(s => s.Id == statusDto.OrderStatusId))
        {
            throw new ArgumentException("Invalid Order Status ID");
        }

        order.OrderStatusId = statusDto.OrderStatusId;
        await _context.SaveChangesAsync();
        await _auditService.LogOrderEventAsync(id, "UpdateStatus", order.UserId, $"Order status changed to {statusDto.OrderStatusId}");
        return true;
    }
}