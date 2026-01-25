namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IDiscountService _discountService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly INotificationService _notificationService;
    private readonly IAuditService _auditService;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderStatusRepository orderStatusRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IPaymentService paymentService,
        IInventoryService inventoryService,
        IDiscountService discountService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger,
        INotificationService notificationService,
        IAuditService auditService,
        IOptions<FrontendUrlsDto> frontendUrls)
    {
        _orderRepository = orderRepository;
        _orderStatusRepository = orderStatusRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _paymentService = paymentService;
        _inventoryService = inventoryService;
        _discountService = discountService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _notificationService = notificationService;
        _auditService = auditService;
        _frontendUrls = frontendUrls;
    }

    public async Task<(IEnumerable<OrderDto> Orders, int TotalItems)> GetOrdersAsync(
        int? userId,
        bool includeDeleted,
        int? currentUserId,
        int? statusId,
        DateTime? fromDate,
        DateTime? toDate,
        int page,
        int pageSize)
    {
        if (userId.HasValue && currentUserId.HasValue && userId != currentUserId)
        {
            userId = currentUserId;
        }

        var (orders, totalItems) = await _orderRepository.GetOrdersAsync(null, includeDeleted, userId, statusId, fromDate, toDate, page, pageSize);

        var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);

        return (orderDtos, totalItems);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id, int? userId, bool isAdmin)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id, userId, !isAdmin);
        if (order == null) return null;

        if (!isAdmin && order.UserId != userId) return null;

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto dto, int userId, string idempotencyKey)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                if (await _orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey))
                {
                    throw new InvalidOperationException("Duplicate request detected.");
                }

                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    throw new ArgumentException("Cart is empty.");
                }

                if (dto.ExpectedItems == null || !dto.ExpectedItems.Any())
                {
                    throw new ArgumentException("ExpectedItems is required for price validation.");
                }

                var cartVariantIds = cart.CartItems.Select(ci => ci.VariantId).OrderBy(x => x).ToList();
                var expectedVariantIds = dto.ExpectedItems.Select(ei => ei.VariantId).OrderBy(x => x).ToList();

                if (!cartVariantIds.SequenceEqual(expectedVariantIds))
                {
                    throw new ArgumentException("ExpectedItems must match cart items exactly.");
                }

                UserAddress? address = null;
                if (dto.NewAddress != null)
                {
                    var addressEntity = _mapper.Map<UserAddress>(dto.NewAddress);
                    addressEntity.UserId = userId;
                    if (dto.SaveNewAddress)
                    {
                        await _userRepository.AddAddressAsync(addressEntity);
                        await _unitOfWork.SaveChangesAsync();
                        address = addressEntity;
                    }
                    else
                    {
                        address = addressEntity;
                        address.Id = 0;
                    }
                }
                else if (dto.UserAddressId.HasValue)
                {
                    address = await _userRepository.GetUserAddressAsync(dto.UserAddressId.Value);
                    if (address == null || address.UserId != userId)
                        throw new ArgumentException("Invalid address.");
                }
                else
                {
                    throw new ArgumentException("Address is required.");
                }

                var variantIds = cart.CartItems.Select(i => i.VariantId).OrderBy(id => id).ToList();
                var variants = await _orderRepository.GetVariantsByIdsForUpdateAsync(variantIds);

                decimal totalAmount = 0;
                decimal totalProfit = 0;
                var orderItems = new List<OrderItem>();

                foreach (var cartItem in cart.CartItems)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == cartItem.VariantId)
                    ?? throw new InvalidOperationException($"Product variant {cartItem.VariantId} not found.");

                    if (!variant.IsActive || variant.IsDeleted || !variant.Product.IsActive || variant.Product.IsDeleted)
                    {
                        throw new InvalidOperationException($"Product '{variant.Product.Name}' is no longer available.");
                    }

                    if (!variant.IsUnlimited && variant.Stock < cartItem.Quantity)
                        throw new DbUpdateConcurrencyException($"Insufficient stock for {variant.Product.Name}.");

                    var expectedItem = dto.ExpectedItems.FirstOrDefault(e => e.VariantId == variant.Id)
                    ?? throw new ArgumentException($"Expected price not provided for variant {variant.Id}.");

                    if (expectedItem.Price != variant.SellingPrice)
                        throw new DbUpdateConcurrencyException($"Price changed for {variant.Product.Name}. Expected: {expectedItem.Price}, Current: {variant.SellingPrice}. Please refresh cart.");

                    var amount = variant.SellingPrice * cartItem.Quantity;
                    var profit = (variant.SellingPrice - variant.PurchasePrice) * cartItem.Quantity;

                    totalAmount += amount;
                    totalProfit += profit;

                    orderItems.Add(new OrderItem
                    {
                        VariantId = variant.Id,
                        Quantity = cartItem.Quantity,
                        SellingPrice = variant.SellingPrice,
                        PurchasePrice = variant.PurchasePrice,
                        Amount = amount,
                        Profit = profit
                    });
                }

                decimal discountAmount = 0;
                int? discountCodeId = null;
                if (!string.IsNullOrEmpty(dto.DiscountCode))
                {
                    var discountResult = await _discountService.ValidateAndApplyDiscountAsync(dto.DiscountCode, totalAmount, userId);
                    if (discountResult.Success && discountResult.Data != null)
                    {
                        discountAmount = discountResult.Data.DiscountAmount;
                        discountCodeId = discountResult.Data.DiscountCodeId;
                    }
                }

                var shippingMethod = await _orderRepository.GetShippingMethodByIdAsync(dto.ShippingMethodId) 
                ?? throw new ArgumentException("Invalid shipping method.");

                var shippingCost = shippingMethod.Cost;

                var initialStatus = (await _orderStatusRepository.GetStatusByNameAsync("Pending Payment")
                                    ?? await _orderStatusRepository.GetStatusByNameAsync("در انتظار پرداخت")
                                    ?? (await _orderStatusRepository.GetOrderStatusesAsync()).FirstOrDefault())
                                    ?? throw new InvalidOperationException("No order status found.");

                var order = new Order
                {
                    UserId = userId,
                    UserAddressId = dto.UserAddressId ?? (dto.SaveNewAddress ? address!.Id : null),
                    ReceiverName = address!.ReceiverName,
                    AddressSnapshot = JsonSerializer.Serialize(_mapper.Map<UserAddressDto>(address)),
                    TotalAmount = totalAmount,
                    TotalProfit = totalProfit,
                    ShippingCost = shippingCost,
                    DiscountAmount = discountAmount,
                    FinalAmount = totalAmount + shippingCost - discountAmount,
                    OrderStatusId = initialStatus.Id,
                    PaymentTransactions = new List<PaymentTransaction>(),
                    ShippingMethodId = shippingMethod.Id,
                    DiscountCodeId = discountCodeId,
                    IdempotencyKey = idempotencyKey,
                    OrderItems = orderItems,
                    IsPaid = false,
                    CreatedAt = DateTime.UtcNow
                };

                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Clear cart immediately after order creation as requested
                await _cartRepository.ClearCartAsync(userId);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in orderItems)
                {
                    await _inventoryService.LogTransactionAsync(
                        item.VariantId,
                        "Reservation",
                        -item.Quantity,
                        item.Id,
                        userId,
                        $"Order Reservation #{order.Id}",
                        $"ORD-{order.Id}",
                        null,
                        false
                    );
                }
                await _unitOfWork.SaveChangesAsync();

                var callbackUrl = dto.CallbackUrl ?? $"{_frontendUrls.Value.BaseUrl}/payment/verify";
                var user = await _userRepository.GetUserByIdAsync(userId);

                var paymentRequest = new PaymentInitiationDto
                {
                    Amount = order.FinalAmount,
                    Description = $"Order #{order.Id}",
                    CallbackUrl = callbackUrl,
                    Mobile = user?.PhoneNumber,
                    OrderId = order.Id,
                    UserId = userId
                };

                var paymentResult = await _paymentService.InitiatePaymentAsync(paymentRequest);

                if (!paymentResult.IsSuccess)
                {
                    throw new Exception($"Payment initiation failed: {paymentResult.Message}");
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return new CheckoutFromCartResultDto
                {
                    OrderId = order.Id,
                    PaymentUrl = paymentResult.PaymentUrl,
                    Authority = paymentResult.Authority
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public async Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var order = await _orderRepository.GetOrderByIdAsync(orderId, null, true);
                if (order == null)
                {
                    return new PaymentVerificationResultDto { IsVerified = false, Message = "Order not found." };
                }

                if (order.IsPaid)
                {
                    return new PaymentVerificationResultDto { IsVerified = true, Message = "Order already paid.", RefId = 0, RedirectUrl = $"/payment/success?orderId={orderId}" };
                }

                var verificationResult = await _paymentService.VerifyPaymentAsync(authority, status);

                if (verificationResult.IsVerified)
                {
                    order.IsPaid = true;
                    order.PaymentDate = DateTime.UtcNow;

                    var paidStatus = await _orderStatusRepository.GetStatusByNameAsync("Processing")
                                     ?? await _orderStatusRepository.GetStatusByNameAsync("در حال پردازش");

                    if (paidStatus != null) order.OrderStatusId = paidStatus.Id;

                    _orderRepository.Update(order);

                    await _unitOfWork.SaveChangesAsync();

                    // Cart is already cleared in CheckoutFromCartAsync, but ensuring it's clean doesn't hurt
                    await _cartRepository.ClearCartAsync(order.UserId);

                    await transaction.CommitAsync();

                    await _notificationService.CreateNotificationAsync(order.UserId, "ثبت سفارش موفق", $"سفارش #{order.Id} با موفقیت ثبت شد.", "Order", $"/dashboard/order/{order.Id}", order.Id, "Order");

                    return new PaymentVerificationResultDto
                    {
                        IsVerified = true,
                        RefId = verificationResult.RefId,
                        Message = "Payment verified successfully.",
                        RedirectUrl = $"/payment/success?orderId={orderId}&refId={verificationResult.RefId}"
                    };
                }
                else
                {
                    foreach (var item in order.OrderItems)
                    {
                        await _inventoryService.LogTransactionAsync(
                            item.VariantId,
                            "Restock",
                            item.Quantity,
                            item.Id,
                            order.UserId,
                            "Payment Failed - Restock",
                            $"ORD-FAIL-{order.Id}",
                            null,
                            false
                        );
                    }

                    var failedStatus = await _orderStatusRepository.GetStatusByNameAsync("Cancelled")
                                       ?? await _orderStatusRepository.GetStatusByNameAsync("لغو شده")
                                       ?? await _orderStatusRepository.GetStatusByNameAsync("Payment Failed");

                    if (failedStatus != null)
                    {
                        order.OrderStatusId = failedStatus.Id;
                    }

                    _orderRepository.Update(order);

                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return new PaymentVerificationResultDto
                    {
                        IsVerified = false,
                        Message = verificationResult.Message ?? "Payment verification failed.",
                        RedirectUrl = $"/payment/failure?orderId={orderId}&reason={verificationResult.Message}"
                    };
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error verifying payment for order {OrderId}", orderId);
                return new PaymentVerificationResultDto { IsVerified = false, Message = "An internal error occurred." };
            }
        });
    }

    public async Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForCartAsync(int userId)
    {
        try
        {
            var cart = await _cartRepository.GetByUserIdAsync(userId);
            if (cart == null || !cart.CartItems.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail("سبد خرید خالی است.");
            }

            var variantIds = cart.CartItems.Select(ci => ci.VariantId).ToList();

            var variantsWithShipping = await _orderRepository.GetVariantsWithShippingMethodsAsync(variantIds);

            if (!variantsWithShipping.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail("محصولات یافت نشدند.");
            }

            var variantsWithoutShipping = variantsWithShipping
                .Where(v => !v.ProductVariantShippingMethods.Any(s => s.IsActive))
                .ToList();

            if (variantsWithoutShipping.Any())
            {
                var productNames = string.Join(", ", variantsWithoutShipping.Select(v => v.Product.Name));
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                    $"برای محصولات زیر هیچ روش ارسالی تعریف نشده است: {productNames}");
            }

            var commonShippingMethods = variantsWithShipping
                .First()
                .ProductVariantShippingMethods
                .Where(s => s.IsActive)
                .Select(s => s.ShippingMethodId)
                .ToHashSet();

            foreach (var variant in variantsWithShipping.Skip(1))
            {
                var variantShippingMethodIds = variant.ProductVariantShippingMethods
                    .Where(s => s.IsActive)
                    .Select(s => s.ShippingMethodId)
                    .ToHashSet();

                commonShippingMethods.IntersectWith(variantShippingMethodIds);
            }

            if (!commonShippingMethods.Any())
            {
                return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                    "هیچ روش ارسال مشترکی بین محصولات سبد خرید شما وجود ندارد. لطفاً با پشتیبانی تماس بگیرید.");
            }

            var totalMultiplier = variantsWithShipping.Sum(v => v.ShippingMultiplier);

            var shippingMethods = await _orderRepository.GetShippingMethodsByIdsAsync(commonShippingMethods.ToList());

            var result = shippingMethods
                .Where(sm => sm.IsActive && !sm.IsDeleted)
                .Select(sm => new AvailableShippingMethodDto
                {
                    Id = sm.Id,
                    Name = sm.Name,
                    BaseCost = sm.Cost,
                    TotalMultiplier = totalMultiplier,
                    FinalCost = sm.Cost * totalMultiplier,
                    Description = sm.Description,
                    EstimatedDeliveryTime = sm.EstimatedDeliveryTime
                })
                .OrderBy(sm => sm.FinalCost)
                .ToList();

            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطا در دریافت روش‌های ارسال قابل انتخاب برای کاربر {UserId}", userId);
            return ServiceResult<IEnumerable<AvailableShippingMethodDto>>.Fail(
                "خطایی در دریافت روش‌های ارسال رخ داد. لطفاً دوباره تلاش کنید.");
        }
    }
}