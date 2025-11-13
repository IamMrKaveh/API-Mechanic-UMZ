namespace MainApi.Services.Order;

public class OrderService : IOrderService
{
    private readonly MechanicContext _context;
    private readonly ILogger<OrderService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IHtmlSanitizer _htmlSanitizer;
    private readonly IZarinpalService _zarinpalService;
    private readonly IConfiguration _configuration;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;

    public OrderService(
        MechanicContext context,
        ILogger<OrderService> logger,
        IRateLimitService rateLimitService,
        IHtmlSanitizer htmlSanitizer,
        IZarinpalService zarinpalService,
        IConfiguration configuration,
        IAuditService auditService,
        ICacheService cacheService,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IMediaService mediaService,
        INotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _htmlSanitizer = htmlSanitizer;
        _zarinpalService = zarinpalService;
        _configuration = configuration;
        _auditService = auditService;
        _cacheService = cacheService;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _mediaService = mediaService;
        _notificationService = notificationService;
    }

    public async Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var query = _context.TOrders
            .Include(o => o.User)
            .Include(o => o.OrderStatus)
            .Include(o => o.OrderItems)
            .Include(o => o.ShippingMethod)
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
                Address = JsonSerializer.Deserialize<UserAddressDto>(o.AddressSnapshot, (JsonSerializerOptions?)null),
                o.TotalAmount,
                o.ShippingCost,
                o.DiscountAmount,
                o.FinalAmount,
                o.CreatedAt,
                User = new
                {
                    o.User.Id,
                    o.User.PhoneNumber,
                    o.User.FirstName,
                    o.User.LastName
                },
                OrderStatus = new
                {
                    o.OrderStatus.Id,
                    o.OrderStatus.Name
                },
                ShippingMethod = new
                {
                    o.ShippingMethod.Id,
                    o.ShippingMethod.Name,
                    o.ShippingMethod.Cost
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
            .Include(o => o.ShippingMethod)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.CategoryGroup.Category)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Variant)
                    .ThenInclude(v => v.VariantAttributes)
                        .ThenInclude(va => va.AttributeValue)
                            .ThenInclude(av => av.AttributeType)
            .Where(o => o.Id == orderId);

        if (!isAdmin)
        {
            query = query.Where(o => o.UserId == currentUserId);
        }

        var orderData = await query.Select(o => new
        {
            o.Id,
            o.UserId,
            Address = JsonSerializer.Deserialize<UserAddressDto>(o.AddressSnapshot, (JsonSerializerOptions?)null),
            o.TotalAmount,
            o.ShippingCost,
            o.DiscountAmount,
            o.FinalAmount,
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
                o.OrderStatus.Name
            } : null,
            ShippingMethod = o.ShippingMethod != null ? new
            {
                o.ShippingMethod.Id,
                o.ShippingMethod.Name,
                o.ShippingMethod.Cost
            } : null,
            OrderItems = o.OrderItems.Select(oi => new
            {
                oi.Id,
                oi.VariantId,
                oi.Quantity,
                oi.SellingPrice,
                PurchasePrice = isAdmin ? (decimal?)oi.PurchasePrice : null,
                oi.Amount,
                oi.Profit,
                ProductId = oi.Variant.Product.Id,
                ProductName = oi.Variant.Product.Name,
                CategoryId = oi.Variant.Product.CategoryGroup.Category.Id,
                CategoryName = oi.Variant.Product.CategoryGroup.Category.Name,
                Attributes = oi.Variant.VariantAttributes
                    .Select(va => new
                    {
                        va.AttributeValueId,
                        TypeName = va.AttributeValue.AttributeType.Name,
                        TypeDisplay = va.AttributeValue.AttributeType.DisplayName,
                        va.AttributeValue.Value,
                        va.AttributeValue.DisplayValue,
                        va.AttributeValue.HexCode
                    }).ToList()
            }).ToList()
        })
        .FirstOrDefaultAsync();

        if (orderData == null)
        {
            return null;
        }

        if (!isAdmin && orderData.UserId != currentUserId)
        {
            return null;
        }

        var enrichedOrderItems = new List<object>();
        foreach (var oi in orderData.OrderItems)
        {
            var icon = await _mediaService.GetPrimaryImageUrlAsync("Product", oi.ProductId);

            enrichedOrderItems.Add(new
            {
                oi.Id,
                oi.VariantId,
                oi.PurchasePrice,
                oi.SellingPrice,
                oi.Quantity,
                oi.Amount,
                oi.Profit,
                Product = new
                {
                    Id = oi.ProductId,
                    Name = oi.ProductName,
                    Icon = icon,
                    Category = new
                    {
                        Id = oi.CategoryId,
                        Name = oi.CategoryName
                    },
                    Attributes = oi.Attributes.ToDictionary(
                        a => a.TypeName.ToLower(),
                        a => new AttributeValueDto(
                            a.AttributeValueId,
                            a.TypeName,
                            a.TypeDisplay,
                            a.Value,
                            a.DisplayValue,
                            a.HexCode
                        ))
                }
            });
        }

        var result = new
        {
            orderData.Id,
            orderData.UserId,
            orderData.Address,
            orderData.TotalAmount,
            orderData.ShippingCost,
            orderData.DiscountAmount,
            orderData.FinalAmount,
            orderData.CreatedAt,
            orderData.OrderStatusId,
            orderData.RowVersion,
            orderData.User,
            orderData.OrderStatus,
            orderData.ShippingMethod,
            OrderItems = enrichedOrderItems
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
                var userAddress = await _context.TUserAddress.FindAsync(orderDto.UserAddressId);
                if (userAddress == null || userAddress.UserId != orderDto.UserId)
                    throw new ArgumentException("Invalid user address");

                var variantIds = orderDto.OrderItems.Select(i => i.VariantId).ToList();
                var variants = await _context.TProductVariant
                    .Where(v => variantIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id, v => v);

                decimal totalAmount = 0;
                decimal totalProfit = 0;
                var orderItems = new List<TOrderItems>();

                foreach (var itemDto in orderDto.OrderItems)
                {
                    if (!variants.TryGetValue(itemDto.VariantId, out var variant))
                        throw new ArgumentException($"Product variant {itemDto.VariantId} not found");

                    await _inventoryService.LogTransactionAsync(variant.Id, "Sale", -itemDto.Quantity, null, orderDto.UserId, $"Order creation", null, variant.RowVersion);

                    var amount = itemDto.SellingPrice * itemDto.Quantity;
                    var profit = (itemDto.SellingPrice - variant.PurchasePrice) * itemDto.Quantity;

                    totalAmount += amount;
                    totalProfit += profit;

                    orderItems.Add(new TOrderItems
                    {
                        VariantId = variant.Id,
                        PurchasePrice = variant.PurchasePrice,
                        SellingPrice = itemDto.SellingPrice,
                        Quantity = itemDto.Quantity,
                        Amount = amount,
                        Profit = profit
                    });
                }

                var shippingMethod = await _context.TShippingMethod.FindAsync(orderDto.ShippingMethodId);
                if (shippingMethod == null) throw new ArgumentException("Invalid shipping method");

                decimal discountAmount = 0;
                int? discountId = null;
                if (!string.IsNullOrEmpty(orderDto.DiscountCode))
                {
                    var (discount, error) = await _discountService.ValidateAndGetDiscountAsync(orderDto.DiscountCode, orderDto.UserId, totalAmount);
                    if (error != null) throw new ArgumentException(error);

                    if (discount != null)
                    {
                        discountAmount = (totalAmount * discount.Percentage) / 100;
                        if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
                        {
                            discountAmount = discount.MaxDiscountAmount.Value;
                        }
                        discountId = discount.Id;
                        discount.UsedCount++;
                    }
                }

                order = new TOrders
                {
                    UserId = orderDto.UserId,
                    AddressSnapshot = JsonSerializer.Serialize(userAddress),
                    UserAddressId = userAddress.Id,
                    CreatedAt = DateTime.UtcNow,
                    OrderStatusId = orderDto.OrderStatusId,
                    IdempotencyKey = idempotencyKey,
                    ShippingMethodId = shippingMethod.Id,
                    ShippingCost = shippingMethod.Cost,
                    DiscountAmount = discountAmount,
                    DiscountCodeId = discountId,
                    TotalAmount = totalAmount,
                    TotalProfit = totalProfit,
                    FinalAmount = totalAmount + shippingMethod.Cost - discountAmount,
                    OrderItems = orderItems
                };

                _context.TOrders.Add(order);
                await _context.SaveChangesAsync();

                if (discountId.HasValue)
                {
                    _context.TDiscountUsage.Add(new TDiscountUsage
                    {
                        UserId = orderDto.UserId,
                        DiscountCodeId = discountId.Value,
                        OrderId = order.Id,
                        DiscountAmount = discountAmount,
                        UsedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

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
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var cart = await _context.TCarts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Variant)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    throw new InvalidOperationException("Cart is empty");

                var userAddress = await _context.TUserAddress.FindAsync(orderDto.UserAddressId);
                if (userAddress == null || userAddress.UserId != userId)
                    throw new ArgumentException("Invalid user address");

                var shippingMethod = await _context.TShippingMethod.FindAsync(orderDto.ShippingMethodId);
                if (shippingMethod == null) throw new ArgumentException("Invalid shipping method");

                const int pendingPaymentStatusId = 1;
                decimal totalAmount = 0;
                decimal totalProfit = 0;

                var orderItems = new List<TOrderItems>();

                foreach (var item in cart.CartItems)
                {
                    var variant = item.Variant;
                    if (variant == null)
                        throw new InvalidOperationException($"Product variant for cart item {item.Id} not found.");

                    await _inventoryService.LogTransactionAsync(variant.Id, "Sale", -item.Quantity, null, userId, $"Checkout for Order", null, variant.RowVersion);

                    var sellingPrice = variant.SellingPrice;
                    var amount = sellingPrice * item.Quantity;
                    var profit = (sellingPrice - variant.PurchasePrice) * item.Quantity;
                    totalAmount += amount;
                    totalProfit += profit;


                    orderItems.Add(new TOrderItems
                    {
                        VariantId = variant.Id,
                        PurchasePrice = variant.PurchasePrice,
                        SellingPrice = sellingPrice,
                        Quantity = item.Quantity,
                        Amount = amount,
                        Profit = profit
                    });
                }

                decimal discountAmount = 0;
                int? discountId = null;
                if (!string.IsNullOrEmpty(orderDto.DiscountCode))
                {
                    var (discount, error) = await _discountService.ValidateAndGetDiscountAsync(orderDto.DiscountCode, userId, totalAmount);
                    if (error != null) throw new ArgumentException(error);

                    if (discount != null)
                    {
                        discountAmount = (totalAmount * discount.Percentage) / 100;
                        if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
                        {
                            discountAmount = discount.MaxDiscountAmount.Value;
                        }
                        discountId = discount.Id;
                        discount.UsedCount++;
                    }
                }

                order = new TOrders
                {
                    UserId = userId,
                    AddressSnapshot = JsonSerializer.Serialize(userAddress),
                    UserAddressId = userAddress.Id,
                    CreatedAt = DateTime.UtcNow,
                    OrderStatusId = pendingPaymentStatusId,
                    IdempotencyKey = idempotencyKey,
                    ShippingMethodId = shippingMethod.Id,
                    ShippingCost = shippingMethod.Cost,
                    DiscountCodeId = discountId,
                    DiscountAmount = discountAmount,
                    TotalAmount = totalAmount,
                    TotalProfit = totalProfit,
                    FinalAmount = totalAmount + shippingMethod.Cost - discountAmount,
                    OrderItems = orderItems,
                    IsPaid = false
                };

                _context.TOrders.Add(order);
                _context.TCartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                if (discountId.HasValue)
                {
                    _context.TDiscountUsage.Add(new TDiscountUsage
                    {
                        UserId = userId,
                        DiscountCodeId = discountId.Value,
                        OrderId = order.Id,
                        DiscountAmount = discountAmount,
                        UsedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Concurrency conflict during checkout for user {UserId}. Rolling back transaction.", userId);
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
            finally
            {
                if (order != null)
                {
                    try
                    {
                        await _auditService.LogOrderEventAsync(order.Id, "CheckoutFromCart", userId, $"Order created with total amount {order.TotalAmount}");
                        await _cacheService.ClearAsync($"cart:user:{userId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to log audit or clear cache after checkout");
                    }
                }
            }
        });

        return order!;
    }

    public async Task<bool> VerifyPaymentAsync(int orderId, string authority)
    {
        var existingTransaction = await _context.TPaymentTransaction
            .FirstOrDefaultAsync(t => t.Authority == authority);

        if (existingTransaction != null)
        {
            if (existingTransaction.Status == "Success")
                return true;

            return false;
        }

        var order = await _context.TOrders.FindAsync(orderId);
        if (order == null || order.IsPaid) return false;

        var finalAmount = order.FinalAmount;

        var verificationResponse = await _zarinpalService.VerifyPaymentAsync(finalAmount, authority);

        var transaction = new TPaymentTransaction
        {
            OrderId = orderId,
            Amount = finalAmount,
            Authority = authority,
            Gateway = "ZarinPal",
            Status = "Initialized",
            CreatedAt = DateTime.UtcNow
        };

        if (verificationResponse != null && (verificationResponse.Code == 100 || verificationResponse.Code == 101))
        {
            order.IsPaid = true;
            order.OrderStatusId = 2;

            transaction.Status = "Success";
            transaction.RefId = verificationResponse.RefID;
            transaction.CardPan = verificationResponse.CardPan;
            transaction.CardHash = verificationResponse.CardHash;
            transaction.Fee = verificationResponse.Fee;
            transaction.VerifiedAt = DateTime.UtcNow;

            await _context.TPaymentTransaction.AddAsync(transaction);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "پرداخت موفق",
                $"پرداخت شما برای سفارش شماره {order.Id} با موفقیت انجام شد.",
                "PaymentSuccess",
                $"/profile/orders/{order.Id}"
            );

            return true;
        }
        else
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = verificationResponse?.Message;
            await _context.TPaymentTransaction.AddAsync(transaction);
            await _context.SaveChangesAsync();
            return false;
        }
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

        if (orderDto.ShippingMethodId.HasValue)
        {
            var shippingMethod = await _context.TShippingMethod.FindAsync(orderDto.ShippingMethodId.Value);
            if (shippingMethod == null) throw new ArgumentException("Invalid shipping method ID");
            order.ShippingMethodId = shippingMethod.Id;
            order.ShippingCost = shippingMethod.Cost;
        }

        if (orderDto.UserAddressId.HasValue)
        {
            var userAddress = await _context.TUserAddress.FirstOrDefaultAsync(ua => ua.Id == orderDto.UserAddressId && ua.UserId == order.UserId);
            if (userAddress == null) throw new ArgumentException("Invalid user address ID");
            order.UserAddressId = userAddress.Id;
            order.AddressSnapshot = JsonSerializer.Serialize(userAddress);
        }

        if (orderDto.DeliveryDate.HasValue)
            order.DeliveryDate = orderDto.DeliveryDate;

        order.FinalAmount = order.TotalAmount + order.ShippingCost - order.DiscountAmount;
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
                    .ThenInclude(oi => oi.Variant)
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
                    await _inventoryService.LogTransactionAsync(orderItem.VariantId, "Return", orderItem.Quantity, orderItem.Id, order.UserId, $"Order Deletion {order.Id}", null, orderItem.Variant.RowVersion);
                }

                _context.TOrderItems.RemoveRange(order.OrderItems);
                _context.TOrders.Remove(order);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                result = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting order {OrderId}", orderId);
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
                TotalRevenue = g.Sum(o => o.FinalAmount),
                AverageOrderValue = g.Average(o => (double)o.FinalAmount)
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
            GeneralStatistics = statistics ?? new { TotalOrders = 0, TotalRevenue = (decimal)0, AverageOrderValue = 0.0 },
            StatusStatistics = statusStatistics
        };
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        var success = false;

        await strategy.ExecuteAsync(async () =>
        {
            var order = await _context.TOrders.FindAsync(id);
            if (order == null)
            {
                success = false;
                return;
            }

            var status = await _context.TOrderStatus.FindAsync(statusDto.OrderStatusId);
            if (status == null)
            {
                throw new ArgumentException("Invalid Order Status ID");
            }

            order.OrderStatusId = statusDto.OrderStatusId;

            try
            {
                await _context.SaveChangesAsync();
                success = true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency exception when updating status for order {OrderId}. Retrying...", id);
                throw;
            }
        });

        if (success)
        {
            var orderForNotification = await _context.TOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
            if (orderForNotification != null)
            {
                var statusName = (await _context.TOrderStatus.FindAsync(statusDto.OrderStatusId))?.Name;
                await _auditService.LogOrderEventAsync(id, "UpdateStatus", orderForNotification.UserId, $"Order status changed to {statusDto.OrderStatusId}");

                await _notificationService.CreateNotificationAsync(
                    orderForNotification.UserId,
                    "تغییر وضعیت سفارش",
                    $"وضعیت سفارش شما با شماره {orderForNotification.Id} به '{statusName}' تغییر کرد.",
                    "OrderStatusChanged",
                    $"/profile/orders/{orderForNotification.Id}"
                );
            }
        }
        return success;
    }
}