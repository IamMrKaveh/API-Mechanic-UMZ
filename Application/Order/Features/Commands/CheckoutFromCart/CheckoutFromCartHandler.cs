namespace Application.Order.Features.Commands.CheckoutFromCart;

public class CheckoutFromCartHandler : IRequestHandler<CheckoutFromCartCommand, ServiceResult<CheckoutResultDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IPaymentService _paymentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly ILogger<CheckoutFromCartHandler> _logger;

    public CheckoutFromCartHandler(
        IOrderRepository orderRepository,
        IShippingMethodRepository shippingMethodRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
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
        _logger = logger;
    }

    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Check Idempotency
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken))
        {
            var existingOrder = await _orderRepository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey, request.UserId, cancellationToken);

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
                var cart = await _cartRepository.GetByUserIdAsync(request.UserId, cancellationToken);
                if (cart == null || !cart.CartItems.Any())
                    return ServiceResult<CheckoutResultDto>.Failure("سبد خرید خالی است.");

                // 3. Validate Address
                UserAddress? userAddress;
                if (request.UserAddressId.HasValue)
                {
                    userAddress = await _userRepository.GetUserAddressAsync(
                        request.UserAddressId.Value, cancellationToken);
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
                        await _userRepository.AddAddressAsync(userAddress, cancellationToken);
                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                    }
                }
                else
                {
                    return ServiceResult<CheckoutResultDto>.Failure("آدرس تحویل الزامی است.");
                }

                // 4. Validate Shipping Method
                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(
                    request.ShippingMethodId, cancellationToken);
                if (shippingMethod == null || !shippingMethod.IsActive)
                    return ServiceResult<CheckoutResultDto>.Failure("روش ارسال انتخاب شده معتبر نیست.");

                // 5. Validate Stock and Prices - Build OrderItemSnapshots
                var orderItemSnapshots = new List<OrderItemSnapshot>();

                foreach (var cartItem in cart.CartItems)
                {
                    var variant = await _cartRepository.GetVariantByIdAsync(cartItem.VariantId, cancellationToken);
                    if (variant == null || !variant.IsActive)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"محصول {cartItem.Variant?.Product?.Name ?? "ناشناخته"} موجود نیست.");

                    if (!variant.IsUnlimited && variant.StockQuantity < cartItem.Quantity)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"موجودی محصول {variant.Product?.Name ?? "ناشناخته"} کافی نیست. موجودی: {variant.StockQuantity}");
                    // Validate expected price
                    var expectedItem = request.ExpectedItems.FirstOrDefault(e => e.VariantId == variant.Id);
                    if (expectedItem != null && expectedItem.ExpectedPrice != variant.SellingPrice)
                        return ServiceResult<CheckoutResultDto>.Failure(
                            $"قیمت محصول {variant.Product?.Name ?? "ناشناخته"} تغییر کرده است. لطفاً سبد خرید را بررسی کنید.");

                    orderItemSnapshots.Add(OrderItemSnapshot.FromVariant(variant, cartItem.Quantity));
                }

                // 6. Validate items using Domain Service
                var itemsValidation = _orderDomainService.ValidateOrderItems(orderItemSnapshots);
                if (!itemsValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(itemsValidation.GetErrorsSummary());

                // 7. Apply Discount
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

                // 8. Create Order using Domain Service
                var order = _orderDomainService.PlaceOrder(
                    request.UserId,
                    userAddress,
                    userAddress.ReceiverName,
                    shippingMethod,
                    request.IdempotencyKey,
                    orderItemSnapshots,
                    discountResult);

                await _orderRepository.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 9. Reserve Inventory
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
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 10. Initiate Payment
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
                var paymentResult = await _paymentService.InitiatePaymentAsync(new PaymentInitiationDto
                {
                    OrderId = order.Id,
                    UserId = request.UserId,
                    Amount = order.FinalAmount,
                    Description = $"پرداخت سفارش #{order.Id}",
                    CallbackUrl = request.CallbackUrl ?? "",
                    Mobile = user?.PhoneNumber
                });

                if (!paymentResult.IsSuccess)
                {
                    await _inventoryService.RollbackReservationsAsync($"ORDER-{order.Id}");
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return ServiceResult<CheckoutResultDto>.Failure(
                        paymentResult.Message ?? "خطا در ایجاد درخواست پرداخت");
                }

                // 11. Clear Cart
                await _cartRepository.ClearCartAsync(request.UserId, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation(
                    "Order {OrderId} created successfully for user {UserId}. OrderNumber: {OrderNumber}",
                    order.Id, request.UserId, order.OrderNumber.Value);

                return ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto
                {
                    OrderId = order.Id,
                    PaymentUrl = paymentResult.PaymentUrl,
                    Authority = paymentResult.Authority
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error during checkout for user {UserId}", request.UserId);
                return ServiceResult<CheckoutResultDto>.Failure("خطایی در فرآیند سفارش رخ داد.");
            }
        }, cancellationToken);
    }
}