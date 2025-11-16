namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderService> _logger;
    private readonly IRateLimitService _rateLimitService;
    private readonly IZarinpalService _zarinpalService;
    private readonly IAuditService _auditService;
    private readonly ICacheService _cacheService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartRepository _cartRepository;
    private readonly FrontendUrlsDto _frontendUrls;
    private readonly ZarinpalSettingsDto _zarinpalSettings;
    private readonly IMapper _mapper;

    public OrderService(
        IOrderRepository orderRepository,
        ILogger<OrderService> logger,
        IRateLimitService rateLimitService,
        IZarinpalService zarinpalService,
        IAuditService auditService,
        ICacheService cacheService,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IMediaService mediaService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ICartRepository cartRepository,
        IOptions<FrontendUrlsDto> frontendUrlsOptions,
        IOptions<ZarinpalSettingsDto> zarinpalSettingsOptions,
        IMapper mapper)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _rateLimitService = rateLimitService;
        _zarinpalService = zarinpalService;
        _auditService = auditService;
        _cacheService = cacheService;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cartRepository = cartRepository;
        _frontendUrls = frontendUrlsOptions.Value;
        _zarinpalSettings = zarinpalSettingsOptions.Value;
        _mapper = mapper;
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
            o.RowVersion,
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
                            a.AttributeValue.HexCode
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
            orderData.RowVersion,
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

    public async Task<(Domain.Order.Order Order, string? PaymentUrl, string? Error)> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey)
    {
        var rateLimitKey = $"checkout_{userId}";
        if (await _rateLimitService.IsLimitedAsync(rateLimitKey, 3, 1))
        {
            _logger.LogWarning("Rate limit exceeded for checkout by user {UserId}", userId);
            throw new Exception("Too many checkout attempts. Please try again in a minute.");
        }

        var existingOrder = await _orderRepository.GetOrderByIdempotencyKey(idempotencyKey, userId);
        if (existingOrder != null)
        {
            _logger.LogInformation("Idempotent checkout request detected for key {IdempotencyKey}, returning existing order {OrderId}", idempotencyKey, existingOrder.Id);
            return (existingOrder, null, "Duplicate request");
        }

        Domain.Order.Order? order = null;
        try
        {
            var cart = await _cartRepository.GetCartAsync(userId);
            if (cart == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty");

            UserAddress? userAddress;
            var user = await _userRepository.GetUserByIdAsync(userId, true);
            if (user == null) throw new ArgumentException("User not found");

            if (orderDto.UserAddressId.HasValue)
            {
                userAddress = await _userRepository.GetUserAddressAsync(orderDto.UserAddressId.Value);
                if (userAddress == null || userAddress.UserId != userId)
                {
                    throw new ArgumentException("Invalid user address");
                }
            }
            else if (orderDto.NewAddress != null)
            {
                userAddress = _mapper.Map<UserAddress>(orderDto.NewAddress);
                if (orderDto.SaveNewAddress)
                {
                    userAddress.UserId = userId;
                    user.UserAddresses.Add(userAddress);
                }
            }
            else
            {
                throw new ArgumentException("An address must be provided.");
            }

            var shippingMethod = await _orderRepository.GetShippingMethodAsync(orderDto.ShippingMethodId);
            if (shippingMethod == null) throw new ArgumentException("Invalid shipping method");

            const int pendingPaymentStatusId = 1;
            decimal totalAmount = 0;
            decimal totalProfit = 0;
            var orderItems = new List<Domain.Order.OrderItem>();

            foreach (var item in cart.CartItems)
            {
                if (!item.Variant.IsUnlimited && item.Variant.Stock < item.Quantity)
                {
                    throw new InvalidOperationException($"کالای '{item.Variant.Product.Name}' به تعداد کافی در انبار موجود نیست.");
                }

                await _inventoryService.LogTransactionAsync(item.Variant.Id, "Sale", -item.Quantity, null, userId, $"Checkout for Order", null, item.Variant.RowVersion);

                var sellingPrice = item.Variant.SellingPrice;
                var amount = sellingPrice * item.Quantity;
                var profit = (sellingPrice - item.Variant.PurchasePrice) * item.Quantity;
                totalAmount += amount;
                totalProfit += profit;

                orderItems.Add(new Domain.Order.OrderItem
                {
                    VariantId = item.Variant.Id,
                    PurchasePrice = item.Variant.PurchasePrice,
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

            order = new Domain.Order.Order
            {
                UserId = userId,
                AddressSnapshot = JsonSerializer.Serialize(userAddress),
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
                FinalAmount = totalAmount + shippingMethod.Cost - discountAmount,
                OrderItems = orderItems,
                IsPaid = false
            };

            await _orderRepository.AddOrderAsync(order);
            _cartRepository.RemoveCartItems(cart.CartItems);

            if (discountId.HasValue)
            {
                await _orderRepository.AddDiscountUsageAsync(new Domain.Discount.DiscountUsage
                {
                    UserId = userId,
                    DiscountCodeId = discountId.Value,
                    OrderId = order.Id,
                    DiscountAmount = discountAmount,
                    UsedAt = DateTime.UtcNow
                });
            }

            await _auditService.LogOrderEventAsync(order.Id, "CheckoutFromCart", userId, $"Order created with total amount {order.TotalAmount}");
            await _cacheService.ClearAsync($"cart:user:{userId}");

            await _unitOfWork.SaveChangesAsync();

            try
            {
                var callbackUrl = $"{_frontendUrls.BaseUrl}/payment-verify?orderId={order.Id}";
                var paymentResponse = await _zarinpalService.CreatePaymentRequestAsync(_zarinpalSettings, order.FinalAmount, $"پرداخت سفارش شماره {order.Id}", callbackUrl, order.User?.PhoneNumber);

                if (paymentResponse?.Data?.Code == 100 && !string.IsNullOrEmpty(paymentResponse.Data.Authority))
                {
                    var gatewayUrl = _zarinpalService.GetPaymentGatewayUrl(_zarinpalSettings.IsSandbox, paymentResponse.Data.Authority);
                    return (order, gatewayUrl, null);
                }

                var message = paymentResponse?.Data?.Message ?? "Failed to generate payment link.";
                _logger.LogError("Zarinpal payment URL is null for order {OrderId}. Reason: {Reason}", order.Id, message);
                return (order, null, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create payment request for order {OrderId}", order.Id);
                return (order, null, "Failed to contact payment provider.");
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict during checkout for user {UserId}. Rolling back transaction.", userId);
            throw;
        }
    }

    public async Task<(bool IsVerified, string RedirectUrl)> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        string frontendUrl = _frontendUrls.BaseUrl;

        if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(status))
        {
            return (false, $"{frontendUrl}/payment/failure?reason=invalidparams&orderId={orderId}");
        }

        var order = await _orderRepository.GetOrderForPaymentAsync(orderId);
        if (order == null)
        {
            return (false, $"{frontendUrl}/payment/failure?reason=notfound&orderId={orderId}");
        }
        if (order.IsPaid)
        {
            return (true, $"{frontendUrl}/payment/success?orderId={orderId}&authority={authority}");
        }

        if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase))
        {
            var transaction = await _orderRepository.GetPaymentTransactionAsync(authority);
            var reason = transaction?.ErrorMessage ?? "payment_failed";
            return (false, $"{frontendUrl}/payment/failure?orderId={orderId}&authority={authority}&status={status}&reason={reason}");
        }

        var isVerified = await VerifyPaymentInternalAsync(orderId, authority);
        if (isVerified)
        {
            return (true, $"{frontendUrl}/payment/success?orderId={orderId}&authority={authority}");
        }

        var failedTransaction = await _orderRepository.GetPaymentTransactionAsync(authority);
        var failureReason = failedTransaction?.ErrorMessage ?? "verification_failed";
        return (false, $"{frontendUrl}/payment/failure?orderId={orderId}&authority={authority}&status=failed&reason={failureReason}");
    }

    private async Task<bool> VerifyPaymentInternalAsync(int orderId, string authority)
    {
        var existingTransaction = await _orderRepository.GetPaymentTransactionAsync(authority);

        if (existingTransaction != null)
        {
            return existingTransaction.Status == "Success";
        }

        var order = await _orderRepository.GetOrderForPaymentAsync(orderId);
        if (order == null || order.IsPaid) return false;

        var finalAmount = order.FinalAmount;

        var verificationResponse = await _zarinpalService.VerifyPaymentAsync(_zarinpalSettings, finalAmount, authority);

        var transaction = new Domain.Payment.PaymentTransaction
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

            await _orderRepository.AddPaymentTransactionAsync(transaction);

            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "پرداخت موفق",
                $"پرداخت شما برای سفارش شماره {order.Id} با موفقیت انجام شد.",
                "PaymentSuccess",
                $"/profile/orders/{order.Id}"
            );

            await _unitOfWork.SaveChangesAsync();
            return true;
        }
        else
        {
            transaction.Status = "Failed";
            transaction.ErrorMessage = verificationResponse?.Message;
            await _orderRepository.AddPaymentTransactionAsync(transaction);
            await _unitOfWork.SaveChangesAsync();
            return false;
        }
    }
}