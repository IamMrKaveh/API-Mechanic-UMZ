namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingRepository _shippingMethodRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly InventoryReservationService _inventoryReservationService;
    private readonly ILogger<CheckoutFromCartHandler> _logger;

    public CheckoutFromCartHandler(
        IOrderRepository orderRepository,
        IShippingRepository shippingMethodRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
        InventoryReservationService inventoryReservationService,
        ILogger<CheckoutFromCartHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _unitOfWork = unitOfWork;
        _orderDomainService = orderDomainService;
        _inventoryReservationService = inventoryReservationService;
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

                // 5. Build OrderItemSnapshots + بارگذاری Variantها
                var orderItemSnapshots = new List<OrderItemSnapshot>();
                var variantItems = new List<(ProductVariant Variant, int Quantity)>();

                foreach (var cartItem in cart.CartItems)
                {
                    var variant = await _cartRepository.GetVariantByIdAsync(cartItem.VariantId, ct);
                    if (variant == null || !variant.IsActive)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"محصول {cartItem.Variant?.Product?.Name ?? "ناشناخته"} موجود نیست.");

                    // Validate expected price
                    var expectedItem = request.ExpectedItems.FirstOrDefault(e => e.VariantId == variant.Id);
                    if (expectedItem != null && expectedItem.ExpectedPrice != variant.SellingPrice)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"قیمت محصول {variant.Product?.Name ?? "ناشناخته"} تغییر کرده است. لطفاً سبد خرید را بررسی کنید.");

                    variantItems.Add((variant, cartItem.Quantity));
                    orderItemSnapshots.Add(OrderItemSnapshot.FromVariant(variant, cartItem.Quantity));
                }

                // 6. اعتبارسنجی موجودی از طریق Domain Service - Business Rule از Handler حذف شد
                var stockValidation = _inventoryReservationService.ValidateBatchAvailability(variantItems);
                if (!stockValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(stockValidation.GetErrorsSummary());

                // 7. Validate items using Domain Service
                var itemsValidation = _orderDomainService.ValidateOrderItems(orderItemSnapshots);
                if (!itemsValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(itemsValidation.GetErrorsSummary());

                // 8. Apply Discount
                DiscountApplicationResult? discountResult = null;

                if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                {
                    var totalAmount = orderItemSnapshots.Sum(x => x.SellingPrice.Amount * x.Quantity);
                    var discountServiceResult = await _discountService.ValidateAndApplyDiscountAsync(
                        request.DiscountCode, totalAmount, request.UserId);

                    if (discountServiceResult.IsSucceed && discountServiceResult.Data != null)
                    {
                        discountResult = DiscountApplicationResult.Success(
                            discountServiceResult.Data.DiscountCodeId,
                            Money.FromDecimal(discountServiceResult.Data.DiscountAmount));
                    }
                }

                // 9. Create Order using Domain Service
                var order = _orderDomainService.PlaceOrder(
                    request.UserId,
                    userAddress,
                    userAddress.ReceiverName,
                    shippingMethod,
                    request.IdempotencyKey,
                    orderItemSnapshots,
                    discountResult);

                await _orderRepository.AddAsync(order, ct);
                await _unitOfWork.SaveChangesAsync(ct);

                // 10. Reserve Inventory
                foreach (var orderItem in order.OrderItems)
                {
                    await _inventoryService.LogTransactionAsync(
                        orderItem.VariantId,
                        "Reservation",
                        -orderItem.Quantity,
                        orderItem.Id,
                        request.UserId,
                        $"Reserved for order {order.Id}",
                        $"ORDER-{order.Id}",
                        null,
                        false);
                }
                await _unitOfWork.SaveChangesAsync(ct);

                // 11. Initiate Payment
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

                // 12. Clear Cart
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