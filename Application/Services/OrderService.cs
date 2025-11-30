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
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly FrontendUrlsDto _frontendUrls;
    private readonly ZarinpalSettingsDto _zarinpalSettings;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

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
        IOrderStatusRepository orderStatusRepository,
        IOptions<FrontendUrlsDto> frontendUrlsOptions,
        IOptions<ZarinpalSettingsDto> zarinpalSettingsOptions,
        IMapper mapper,
        ICurrentUserService currentUserService)
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
        _orderStatusRepository = orderStatusRepository;
        _frontendUrls = frontendUrlsOptions.Value;
        _zarinpalSettings = zarinpalSettingsOptions.Value;
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

            try
            {
                var amountInRials = Convert.ToInt64(order.FinalAmount * 10);
                var callbackUrl = $"{_frontendUrls.BaseUrl}/payment/callback?orderId={order.Id}";
                var userPhone = await _userRepository.GetUserByIdAsync(userId);

                var paymentResponse = await _zarinpalService.CreatePaymentRequestAsync(
                    _zarinpalSettings,
                    amountInRials,
                    $"پرداخت سفارش شماره {order.Id}",
                    callbackUrl,
                    userPhone?.PhoneNumber
                );

                if (paymentResponse?.Data?.Code == 100 && !string.IsNullOrEmpty(paymentResponse.Data.Authority))
                {
                    var paymentTx = new PaymentTransaction
                    {
                        OrderId = order.Id,
                        Authority = paymentResponse.Data.Authority,
                        Amount = amountInRials,
                        Gateway = "ZarinPal",
                        Status = "Initialized",
                        CreatedAt = DateTime.UtcNow,
                        IpAddress = _currentUserService.IpAddress
                    };

                    await _orderRepository.AddPaymentTransactionAsync(paymentTx);
                    await _unitOfWork.SaveChangesAsync();

                    var gatewayUrl = _zarinpalService.GetPaymentGatewayUrl(_zarinpalSettings.IsSandbox, paymentResponse.Data.Authority);
                    return new CheckoutFromCartResultDto { OrderId = order.Id, PaymentUrl = gatewayUrl, Authority = paymentResponse.Data.Authority };
                }
                else
                {
                    var failedStatus = await _orderStatusRepository.GetStatusByNameAsync("PaymentInitializationFailed");
                    if (failedStatus != null)
                    {
                        order.OrderStatusId = failedStatus.Id;
                        _orderRepository.Update(order);
                        await _unitOfWork.SaveChangesAsync();
                    }

                    await _auditService.LogOrderEventAsync(order.Id, "PaymentInitFailed", userId, "Zarinpal authority not received.");
                    return new CheckoutFromCartResultDto { OrderId = order.Id, Error = "خطا در ایجاد تراکنش بانکی. لطفا مجددا تلاش کنید." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing payment for order {OrderId}", order.Id);

                var failedStatus = await _orderStatusRepository.GetStatusByNameAsync("PaymentInitializationFailed");
                if (failedStatus != null)
                {
                    order.OrderStatusId = failedStatus.Id;
                    _orderRepository.Update(order);
                    await _unitOfWork.SaveChangesAsync();
                }
                await _auditService.LogOrderEventAsync(order.Id, "PaymentInitError", userId, ex.Message);
                return new CheckoutFromCartResultDto { OrderId = order.Id, Error = "خطا در ارتباط با درگاه پرداخت." };
            }
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey);
        }
    }

    public async Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        string frontendUrl = _frontendUrls.BaseUrl;

        if (string.IsNullOrEmpty(authority) || string.IsNullOrEmpty(status))
        {
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?reason=invalidparams&orderId={orderId}",
                Message = "پارامترهای نامعتبر"
            };
        }

        var currentUserId = _currentUserService.UserId;

        // Locking to prevent concurrent verification
        var lockKey = $"verify:{orderId}";
        if (!await _cacheService.AcquireLockAsync(lockKey, TimeSpan.FromSeconds(30)))
        {
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?reason=processing&orderId={orderId}",
                Message = "تراکنش در حال پردازش است."
            };
        }

        try
        {
            var order = await _orderRepository.GetOrderForPaymentAsync(orderId);
            if (order == null)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=notfound&orderId={orderId}",
                    Message = "سفارش یافت نشد"
                };
            }

            if (currentUserId.HasValue && order.UserId != currentUserId.Value && !_currentUserService.IsAdmin)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?reason=forbidden&orderId={orderId}",
                    Message = "عدم دسترسی"
                };
            }

            if (order.IsPaid)
            {
                var paidTransaction = await _orderRepository.GetPaymentTransactionAsync(authority);
                return new PaymentVerificationResultDto
                {
                    IsVerified = true,
                    RedirectUrl = $"{frontendUrl}/payment/success?orderId={orderId}&authority={authority}&status=ok",
                    RefId = paidTransaction?.RefId,
                    Message = "پرداخت قبلا انجام شده است"
                };
            }

            if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase))
            {
                var transaction = await _orderRepository.GetPaymentTransactionAsync(authority);
                var reason = transaction?.ErrorMessage ?? "payment_failed";
                // Log failure
                await _auditService.LogOrderEventAsync(orderId, "PaymentFailedCallback", order.UserId, $"Status: {status}");
                return new PaymentVerificationResultDto
                {
                    IsVerified = false,
                    RedirectUrl = $"{frontendUrl}/payment/failure?orderId={orderId}&authority={authority}&status={status}&reason={reason}",
                    Message = "پرداخت ناموفق بود"
                };
            }

            var verificationResult = await VerifyPaymentInternalAsync(orderId, authority);
            if (verificationResult.IsVerified)
            {
                return new PaymentVerificationResultDto
                {
                    IsVerified = true,
                    RedirectUrl = $"{frontendUrl}/payment/success?orderId={orderId}&authority={authority}&status=ok",
                    RefId = verificationResult.RefId,
                    Message = "پرداخت با موفقیت انجام شد"
                };
            }

            var failedTransaction = await _orderRepository.GetPaymentTransactionAsync(authority);
            var failureReason = failedTransaction?.ErrorMessage ?? "verification_failed";
            return new PaymentVerificationResultDto
            {
                IsVerified = false,
                RedirectUrl = $"{frontendUrl}/payment/failure?orderId={orderId}&authority={authority}&status=failed&reason={failureReason}",
                Message = "تایید پرداخت ناموفق بود"
            };
        }
        finally
        {
            await _cacheService.ReleaseLockAsync(lockKey);
        }
    }

    private async Task<(bool IsVerified, long? RefId)> VerifyPaymentInternalAsync(int orderId, string authority)
    {
        using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var existingTx = await _orderRepository.GetPaymentTransactionAsync(authority);
            if (existingTx != null)
            {
                existingTx.VerificationCount++;
                existingTx.VerificationAttemptedAt = DateTime.UtcNow;

                if (existingTx.Status == "Success")
                {
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return (true, existingTx.RefId);
                }
            }

            var order = await _orderRepository.GetOrderForPaymentAsync(orderId);
            if (order == null || order.IsPaid)
            {
                await transaction.RollbackAsync();
                return (order?.IsPaid ?? false, existingTx?.RefId);
            }

            var amountInRials = Convert.ToInt64(order.FinalAmount * 10);

            // Verify with Zarinpal
            var verificationResponse =
                await _zarinpalService.VerifyPaymentAsync(_zarinpalSettings, amountInRials, authority);

            var paymentTx = existingTx ?? new PaymentTransaction
            {
                OrderId = orderId,
                Amount = amountInRials,
                Authority = authority,
                Gateway = "ZarinPal",
                Status = "Initialized",
                CreatedAt = DateTime.UtcNow,
                VerificationAttemptedAt = DateTime.UtcNow,
                VerificationCount = 1
            };

            // Handle 101 - Already verified
            bool paymentSuccess = verificationResponse != null && (verificationResponse.Code == 100 || verificationResponse.Code == 101);

            // Amount Mismatch Check
            if (paymentSuccess)
            {
                // Assuming Zarinpal returns the amount, but verificationResponse usually doesn't confirm amount in simple DTO.
                // Trusting our amount passed. If Zarinpal 100/101, it matches the amount we sent.
            }

            if (paymentSuccess)
            {
                var paidStatus = await _orderStatusRepository.GetStatusByNameAsync("Paid");
                if (paidStatus == null)
                {
                    _logger.LogCritical("Order status 'Paid' not found in DB.");
                    await transaction.RollbackAsync();
                    return (false, null);
                }

                order.IsPaid = true;
                order.OrderStatusId = paidStatus.Id;

                paymentTx.Status = "Success";
                paymentTx.RefId = verificationResponse?.RefID;
                paymentTx.CardPan = verificationResponse?.CardPan;
                paymentTx.CardHash = verificationResponse?.CardHash;
                paymentTx.Fee = verificationResponse?.Fee ?? 0;
                paymentTx.VerifiedAt = DateTime.UtcNow;

                if (existingTx == null)
                {
                    await _orderRepository.AddPaymentTransactionAsync(paymentTx);
                }

                if (order.DiscountCodeId.HasValue)
                {
                    var orderWithUsages = await _orderRepository.GetOrderWithItemsAsync(orderId);

                    if (orderWithUsages?.DiscountUsages != null)
                    {
                        foreach (var usage in orderWithUsages.DiscountUsages)
                        {
                            usage.IsConfirmed = true;

                            if (usage.DiscountCode != null)
                            {
                                usage.DiscountCode.UsedCount++;
                            }
                        }
                    }
                }

                // Clear cart and notifications inside same transaction
                var cart = await _cartRepository.GetCartAsync(order.UserId);
                if (cart != null && cart.CartItems.Any())
                {
                    _cartRepository.RemoveCartItems(cart.CartItems);
                    await _cacheService.ClearAsync($"cart:user:{order.UserId}");
                }

                await _notificationService.CreateNotificationAsync(
                    order.UserId,
                    "پرداخت موفق",
                    $"پرداخت شما برای سفارش شماره {order.Id} با موفقیت انجام شد.",
                    "PaymentSuccess",
                    $"/profile/orders/{order.Id}"
                );

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                await _auditService.LogOrderEventAsync(orderId, "PaymentVerified", order.UserId, $"RefID: {paymentTx.RefId}");

                return (true, verificationResponse?.RefID);
            }

            paymentTx.Status = "Failed";
            paymentTx.ErrorMessage = verificationResponse?.Message;

            if (existingTx == null)
            {
                await _orderRepository.AddPaymentTransactionAsync(paymentTx);
            }

            await _unitOfWork.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditService.LogOrderEventAsync(orderId, "PaymentVerificationFailed", order.UserId, $"Error: {paymentTx.ErrorMessage}");

            return (false, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error verifying payment for order {OrderId}", orderId);
            await _auditService.LogOrderEventAsync(orderId, "PaymentVerificationError", 0, ex.Message);
            return (false, null);
        }
    }
}