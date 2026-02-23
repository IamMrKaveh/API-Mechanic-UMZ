namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiscountRepository _discountRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly InventoryReservationService _inventoryReservationService;
    private readonly DiscountApplicationService _discountApplicationService;
    private readonly ILogger<CheckoutFromCartHandler> _logger;

    public CheckoutFromCartHandler(
        IOrderRepository orderRepository,
        IShippingRepository shippingMethodRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IDiscountRepository discountRepository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
        InventoryReservationService inventoryReservationService,
        DiscountApplicationService discountApplicationService,
        ILogger<CheckoutFromCartHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _discountRepository = discountRepository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _orderDomainService = orderDomainService;
        _inventoryReservationService = inventoryReservationService;
        _discountApplicationService = discountApplicationService;
        _logger = logger;
    }

    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken ct)
    {
        // 1. Check Idempotency
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, ct))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey, request.UserId, ct);

            if (existingOrder != null)
            {
                _logger.LogInformation("Duplicate checkout request for idempotency key: {Key}", request.IdempotencyKey);
                return ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto
                {
                    OrderId = existingOrder.Id,
                    Error = "Order already exists for this request"
                });
            }
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                // 2. Validate Cart
                var cart = await _cartRepository.GetByUserIdAsync(request.UserId, ct);
                if (cart == null || !cart.CartItems.Any())
                    return ServiceResult<CheckoutResultDto>.Failure("سبد خرید خالی است.");

                // 3. Validate Address
                UserAddress? userAddress;
                if (request.UserAddressId.HasValue)
                {
                    userAddress = await _userRepository.GetUserAddressAsync(request.UserAddressId.Value, ct);
                    if (userAddress == null || userAddress.UserId != request.UserId)
                        return ServiceResult<CheckoutResultDto>.Failure("آدرس انتخاب شده معتبر نیست.");
                }
                else if (request.NewAddress != null)
                {
                    userAddress = UserAddress.Create(
                        request.UserId,
                        request.NewAddress.Title,
                        request.NewAddress.ReceiverName,
                        request.NewAddress.PhoneNumber,
                        request.NewAddress.Province,
                        request.NewAddress.City,
                        request.NewAddress.Address,
                        request.NewAddress.PostalCode,
                        request.NewAddress.IsDefault);

                    if (request.SaveNewAddress)
                    {
                        await _userRepository.AddAddressAsync(userAddress, ct);
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                }
                else
                {
                    return ServiceResult<CheckoutResultDto>.Failure("آدرس تحویل الزامی است.");
                }

                // 4. Validate Shipping Method
                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(request.ShippingId, ct);
                if (shippingMethod == null || !shippingMethod.IsActive)
                    return ServiceResult<CheckoutResultDto>.Failure("روش ارسال انتخاب شده معتبر نیست.");

                // 5. Build OrderItemSnapshots + load Variants (no decision-making here)
                var orderItemSnapshots = new List<OrderItemSnapshot>();
                var variantItems = new List<(ProductVariant Variant, int Quantity)>();

                foreach (var cartItem in cart.CartItems)
                {
                    var variant = await _cartRepository.GetVariantByIdAsync(cartItem.VariantId, ct);
                    if (variant == null || !variant.IsActive)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"محصول ناشناخته یا غیرفعال در سبد خرید وجود دارد.");

                    variantItems.Add((variant, cartItem.Quantity));
                    orderItemSnapshots.Add(OrderItemSnapshot.FromVariant(variant, cartItem.Quantity));
                }

                // 6. Validate price integrity via Domain Service (replaces handler-level if/else)
                var priceExpectations = variantItems
                    .Select(vi => (
                        vi.Variant,
                        ExpectedPrice: request.ExpectedItems
                            .FirstOrDefault(e => e.VariantId == vi.Variant.Id)?.ExpectedPrice
                            ?? vi.Variant.SellingPrice.Amount))
                    .ToList();

                var priceValidation = _orderDomainService.ValidatePriceIntegrity(priceExpectations);
                if (!priceValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(priceValidation.GetErrorsSummary());

                // 7. Validate stock availability via Domain Service (unchanged)
                var stockValidation = _inventoryReservationService.ValidateBatchAvailability(variantItems);
                if (!stockValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(stockValidation.GetErrorsSummary());

                // 8. Validate items using Domain Service
                var itemsValidation = _orderDomainService.ValidateOrderItems(orderItemSnapshots);
                if (!itemsValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(itemsValidation.GetErrorsSummary());

                // 9. Create Order via Domain Service
                var order = _orderDomainService.PlaceOrder(
                    request.UserId,
                    userAddress,
                    userAddress.ReceiverName,
                    shippingMethod,
                    request.IdempotencyKey,
                    orderItemSnapshots,
                    discountResult: null);

                await _orderRepository.AddAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                // 10. Apply Discount via Domain Service
                if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                {
                    var discountCode = await _discountRepository.GetByCodeAsync(request.DiscountCode, ct);
                    if (discountCode == null)
                        return ServiceResult<CheckoutResultDto>.Failure("کد تخفیف نامعتبر است.");

                    var userUsageCount = await _discountRepository.CountUserUsageAsync(
                        discountCode.Id, request.UserId, ct);

                    var discountResult = _discountApplicationService.ApplyToOrder(
                        discountCode, order, request.UserId, userUsageCount);

                    if (!discountResult.IsSuccess)
                        return ServiceResult<CheckoutResultDto>.Failure(discountResult.Error!);

                    _discountRepository.Update(discountCode);
                    await _orderRepository.UpdateAsync(order, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                // 11. Reserve Inventory via Application Service
                var correlationPrefix = $"CHECKOUT-{order.Id}";
                foreach (var orderItem in order.OrderItems)
                {
                    var reserveResult = await _inventoryService.ReserveStockAsync(
                        orderItem.VariantId,
                        orderItem.Quantity,
                        orderItem.Id,
                        request.UserId,
                        $"ORDER-{order.Id}",
                        $"{correlationPrefix}-{orderItem.VariantId}",
                        ct: ct);

                    if (reserveResult.IsFailed)
                    {
                        await _inventoryService.RollbackReservationsAsync($"ORDER-{order.Id}");
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"خطا در رزرو موجودی: {reserveResult.Error}");
                    }
                }
                await _unitOfWork.SaveChangesAsync(ct);

                // 12. Initiate Payment
                var user = await _userRepository.GetByIdAsync(request.UserId, ct);
                var paymentResult = await _paymentService.InitiatePaymentAsync(new PaymentInitiationDto
                {
                    OrderId = order.Id,
                    UserId = request.UserId,
                    Amount = order.FinalAmount,
                    Description = $"پرداخت سفارش #{order.Id}",
                    CallbackUrl = request.CallbackUrl ?? "",
                    Mobile = user?.PhoneNumber
                });

                if (paymentResult.IsFailed)
                {
                    await _inventoryService.RollbackReservationsAsync($"ORDER-{order.Id}");
                    await _unitOfWork.RollbackTransactionAsync(ct);

                    return ServiceResult<CheckoutResultDto>.Failure(
                        paymentResult.Error ?? "خطا در ایجاد درخواست پرداخت");
                }

                // 13. Clear Cart
                await _cartRepository.ClearCartAsync(request.UserId, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                await _unitOfWork.CommitTransactionAsync(ct);

                _logger.LogInformation(
                    "Order {OrderId} created successfully for user {UserId}. OrderNumber: {OrderNumber}",
                    order.Id, request.UserId, order.OrderNumber.Value);

                return ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto
                {
                    OrderId = order.Id,
                    PaymentUrl = paymentResult.Data.PaymentUrl,
                    Authority = paymentResult.Data.Authority,
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(ct);
                _logger.LogError(ex, "Error during checkout for user {UserId}", request.UserId);
                return ServiceResult<CheckoutResultDto>.Failure("خطایی در فرآیند سفارش رخ داد.");
            }
        }, ct);
    }
}