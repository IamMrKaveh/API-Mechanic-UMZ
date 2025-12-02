namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IPaymentService _paymentService;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartRepository _cartRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public OrderService(
    IOrderRepository orderRepository,
    ILogger<OrderService> logger,
    IRateLimitService rateLimitService,
    IPaymentService paymentService,
    IAuditService auditService,
    ICacheService cacheService,
    IDiscountService discountService,
    IInventoryService inventoryService,
    IMediaService mediaService,
    INotificationService notificationService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ICartRepository cartRepository,
    IOrderStatusRepository orderStatusRepository,
    IMapper mapper,
    ICurrentUserService currentUserService)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _paymentService = paymentService;
        _auditService = auditService;
        _cacheService = cacheService;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cartRepository = cartRepository;
        _orderStatusRepository = orderStatusRepository;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    public async Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var (orders, totalItems) = await _orderRepository.GetOrdersAsync(currentUserId, isAdmin, userId, statusId, fromDate, toDate, page, pageSize);

        var orderDtos = orders.Select(o => new
        {
            o.Id,
            AddressSnapshot = JsonSerializer.Deserialize<UserAddressDto>(o.AddressSnapshot, (JsonSerializerOptions?)null),
            o.TotalAmount,
            TotalProfit = isAdmin ? (decimal?)o.TotalProfit : null,
            o.ShippingCost,
            o.DiscountAmount,
            o.FinalAmount,
            o.CreatedAt,
            RowVersion = o.RowVersion != null ? Convert.ToBase64String(o.RowVersion) : null,
            o.OrderStatusId,
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
                o.OrderStatus.Name,
                o.OrderStatus.Icon
            },
            ShippingMethod = new
            {
                o.ShippingMethod.Id,
                o.ShippingMethod.Name,
                o.ShippingMethod.Cost
            },
            OrderItemsCount = o.OrderItems.Count
        });

        return (orderDtos, totalItems);
    }

    public async Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin)
    {
        var orderData = await _orderRepository.GetOrderByIdAsync(orderId, currentUserId, isAdmin);

        if (orderData == null)
        {
            return null;
        }

        var enrichedOrderItems = new List<object>();
        foreach (var oi in orderData.OrderItems)
        {
            var icon = await _mediaService.GetPrimaryImageUrlAsync("Product", oi.Variant.ProductId);

            enrichedOrderItems.Add(new
            {
                oi.Id,
                oi.VariantId,
                PurchasePrice = isAdmin ? (decimal?)oi.PurchasePrice : null,
                oi.SellingPrice,
                oi.Quantity,
                oi.Amount,
                Profit = isAdmin ? (decimal?)oi.Profit : null,
                Product = new
                {
                    Id = oi.Variant.ProductId,
                    Name = oi.Variant.Product.Name,
                    Icon = icon,
                    Category = new
                    {
                        Id = oi.Variant.Product.CategoryGroup.CategoryId,
                        Name = oi.Variant.Product.CategoryGroup.Category.Name
                    },
                    Attributes = oi.Variant.VariantAttributes.ToDictionary(
                        a => a.AttributeValue.AttributeType.Name.ToLower(),
                        a => new AttributeValueDto(
                            a.AttributeValue.Id,
                            a.AttributeValue.AttributeType.Name,
                            a.AttributeValue.AttributeType.DisplayName,
                            a.AttributeValue.Value,
                            a.AttributeValue.DisplayValue,
                            a.AttributeValue.HexCode ?? string.Empty
                        ))
                }
            });
        }

        var result = new
        {
            orderData.Id,
            orderData.UserId,
            AddressSnapshot = JsonSerializer.Deserialize<UserAddressDto>(orderData.AddressSnapshot, (JsonSerializerOptions?)null),
            orderData.TotalAmount,
            TotalProfit = isAdmin ? (decimal?)orderData.TotalProfit : null,
            orderData.ShippingCost,
            orderData.DiscountAmount,
            orderData.FinalAmount,
            orderData.CreatedAt,
            orderData.OrderStatusId,
            orderData.IsPaid,
            RowVersion = orderData.RowVersion != null ? Convert.ToBase64String(orderData.RowVersion) : null,
            User = orderData.User != null ? new
            {
                orderData.User.Id,
                orderData.User.PhoneNumber,
                orderData.User.FirstName,
                orderData.User.LastName,
                orderData.User.IsAdmin
            } : null,
            OrderStatus = orderData.OrderStatus,
            ShippingMethod = orderData.ShippingMethod,
            OrderItems = enrichedOrderItems
        };

        return result;
    }

    public async Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey)
    {
        var rateLimitKey = $"checkout_{userId}";
        (bool isLimited, int retryAfterSeconds) = await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 1);
        if (isLimited)
        {
            _logger.LogWarning("Rate limit exceeded for checkout by user {UserId}", userId);
            throw new InvalidOperationException("تعداد درخواست‌های شما زیاد است. لطفا یک دقیقه صبر کنید.");
        }

        var preCheckCart = await _cartRepository.GetCartAsync(userId);
        if (preCheckCart == null || !preCheckCart.CartItems.Any())
            throw new InvalidOperationException("سبد خرید شما خالی است.");

        var lockKey = $"idempotency:{userId}:{idempotencyKey}";
        bool lockAcquired = false;
        int retryCount = 0;
        while (!lockAcquired && retryCount < 5)
        {
            if (await _cacheService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
            {
                lockAcquired = true;
            }
            else
            {
                retryCount++;
                await Task.Delay(500 * retryCount);
            }
        }

        if (!lockAcquired)
        {
            throw new InvalidOperationException("درخواست دیگری در حال پردازش است. لطفا صبر کنید.");
        }

        try
        {
            Order order = await _unitOfWork.ExecuteStrategyAsync(async () =>
            {
                using (var transaction = await _unitOfWork.BeginTransactionAsync())
                {
                    try
                    {
                        var cart = await _cartRepository.GetCartAsync(userId);
                        if (cart == null || !cart.CartItems.Any())
                            throw new InvalidOperationException("سبد خرید خالی است");

                        var user = await _userRepository.GetUserByIdAsync(userId, true);
                        if (user == null) throw new ArgumentException("کاربر یافت نشد");

                        UserAddress? userAddress = null;
                        if (orderDto.UserAddressId.HasValue)
                        {
                            userAddress = await _userRepository.GetUserAddressAsync(orderDto.UserAddressId.Value);
                            if (userAddress == null || userAddress.UserId != userId)
                            {
                                throw new ArgumentException("آدرس انتخاب شده نامعتبر است");
                            }
                        }
                        else if (orderDto.NewAddress != null)
                        {
                            if (string.IsNullOrWhiteSpace(orderDto.NewAddress.Address) ||
                                string.IsNullOrWhiteSpace(orderDto.NewAddress.City) ||
                                string.IsNullOrWhiteSpace(orderDto.NewAddress.PhoneNumber))
                                throw new ArgumentException("اطلاعات آدرس ناقص است.");

                            userAddress = _mapper.Map<UserAddress>(orderDto.NewAddress);
                            if (orderDto.SaveNewAddress == true)
                            {
                                userAddress.UserId = userId;
                                user.UserAddresses.Add(userAddress);
                            }
                        }
                        else
                        {
                            throw new ArgumentException("آدرس ارسال الزامی است.");
                        }

                        var shippingMethod = await _orderRepository.GetShippingMethodAsync(orderDto.ShippingMethodId);
                        if (shippingMethod == null || !shippingMethod.IsActive)
                            throw new ArgumentException("روش ارسال انتخاب شده نامعتبر است");

                        var variantIds = cart.CartItems.Select(ci => ci.VariantId).Distinct().ToList();
                        var variants = await _orderRepository.GetVariantsByIdsForUpdateAsync(variantIds);
                        var variantMap = variants.ToDictionary(v => v.Id);

                        if (orderDto.ExpectedItems != null && orderDto.ExpectedItems.Any())
                        {
                            foreach (var expected in orderDto.ExpectedItems)
                            {
                                if (variantMap.TryGetValue(expected.VariantId, out var v))
                                {
                                    if (v.SellingPrice != expected.Price)
                                        throw new ArgumentException($"قیمت محصول {v.Product.Name} تغییر کرده است. لطفا سبد خرید را بروزرسانی کنید.");
                                }
                            }
                        }

                        int pendingPaymentStatusId = 1;
                        var pendingStatus = await _orderStatusRepository.GetStatusByNameAsync("Pending");
                        if (pendingStatus != null) pendingPaymentStatusId = pendingStatus.Id;

                        decimal totalAmount = 0;
                        decimal totalProfit = 0;
                        var orderItems = new List<OrderItem>();

                        foreach (var item in cart.CartItems)
                        {
                            if (!variantMap.TryGetValue(item.VariantId, out var variant))
                            {
                                throw new InvalidOperationException($"محصول {item.VariantId} دیگر موجود نیست.");
                            }

                            if (!variant.IsUnlimited && variant.Stock < item.Quantity)
                            {
                                throw new InvalidOperationException($"موجودی کافی برای {variant.Product.Name} موجود نیست. موجودی فعلی: {variant.Stock}");
                            }

                            var sellingPrice = variant.SellingPrice;
                            var amount = sellingPrice * item.Quantity;
                            var profit = (sellingPrice - variant.PurchasePrice) * item.Quantity;
                            totalAmount += amount;
                            totalProfit += profit;

                            var orderItem = new OrderItem
                            {
                                VariantId = variant.Id,
                                PurchasePrice = variant.PurchasePrice,
                                SellingPrice = sellingPrice,
                                Quantity = item.Quantity,
                                Amount = amount,
                                Profit = profit
                            };
                            orderItems.Add(orderItem);
                        }

                        decimal discountAmount = 0;
                        int? discountId = null;
                        if (!string.IsNullOrEmpty(orderDto.DiscountCode))
                        {
                            var (discount, error) = await _discountService.ValidateAndGetDiscountAsync(orderDto.DiscountCode, userId, totalAmount);
                            if (error != null)
                            {
                                throw new ArgumentException(error);
                            }

                            if (discount != null)
                            {
                                discountAmount = (totalAmount * discount.Percentage) / 100;
                                if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
                                {
                                    discountAmount = discount.MaxDiscountAmount.Value;
                                }
                                discountId = discount.Id;
                            }
                        }

                        var finalAmount = totalAmount + shippingMethod.Cost - discountAmount;
                        if (finalAmount < 1000) throw new ArgumentException("مبلغ نهایی سفارش معتبر نیست.");

                        var userAddressDto = _mapper.Map<UserAddressDto>(userAddress);
                        var newOrder = new Order
                        {
                            UserId = userId,
                            ReceiverName = userAddress.ReceiverName,
                            AddressSnapshot = JsonSerializer.Serialize(userAddressDto),
                            UserAddressId = userAddress.Id > 0 ? userAddress.Id : null,
                            CreatedAt = DateTime.UtcNow,
                            OrderStatusId = pendingPaymentStatusId,
                            IdempotencyKey = idempotencyKey,
                            ShippingMethodId = shippingMethod.Id,
                            ShippingCost = shippingMethod.Cost,
                            DiscountCodeId = discountId,
                            DiscountAmount = discountAmount,
                            TotalAmount = totalAmount,
                            TotalProfit = totalProfit,
                            FinalAmount = finalAmount,
                            OrderItems = orderItems,
                            IsPaid = false
                        };

                        await _orderRepository.AddOrderAsync(newOrder);
                        await _unitOfWork.SaveChangesAsync();

                        foreach (var oi in newOrder.OrderItems)
                        {
                            await _inventoryService.LogTransactionAsync(
                               oi.VariantId,
                               "Sale",
                               -oi.Quantity,
                               oi.Id,
                               userId,
                               $"Order Checkout #{newOrder.Id}",
                               $"ORDER-{newOrder.Id}",
                               null,
                               false
                           );
                        }

                        if (discountId.HasValue)
                        {
                            await _orderRepository.AddDiscountUsageAsync(new DiscountUsage
                            {
                                UserId = userId,
                                DiscountCodeId = discountId.Value,
                                OrderId = newOrder.Id,
                                DiscountAmount = discountAmount,
                                UsedAt = DateTime.UtcNow,
                                IsConfirmed = false
                            });
                        }

                        await _unitOfWork.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return newOrder;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            });

            var userPhone = await _userRepository.GetUserByIdAsync(userId);
            var (paymentUrl, authority, error) = await _paymentService.InitiatePaymentAsync(
                order.Id,
                userId,
                order.FinalAmount,
                $"پرداخت سفارش شماره {order.Id}",
                userPhone?.PhoneNumber,
                null,
                "ZarinPal" // Default to ZarinPal or dynamic selection
            );

            if (error != null)
            {
                return new CheckoutFromCartResultDto { OrderId = order.Id, Error = error };
            }

            return new CheckoutFromCartResultDto { OrderId = order.Id, PaymentUrl = paymentUrl, Authority = authority };
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey);
        }
    }

    public async Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        return await _paymentService.VerifyPaymentAsync(authority, status);
    }
}