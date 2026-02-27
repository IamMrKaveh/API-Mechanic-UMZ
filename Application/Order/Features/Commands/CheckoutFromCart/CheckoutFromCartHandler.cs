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
    private readonly IWalletRepository _walletRepository;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly InventoryReservationService _inventoryReservationService;
    private readonly ILogger<CheckoutFromCartHandler> _logger;

    public CheckoutFromCartHandler(
        IOrderRepository orderRepository,
        IShippingRepository shippingMethodRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IDiscountRepository discountRepository,
        IInventoryService inventoryService,
        IPaymentService paymentService,
        IWalletRepository walletRepository,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
        InventoryReservationService inventoryReservationService,
        ILogger<CheckoutFromCartHandler> logger)
    {
        _orderRepository = orderRepository;
        _shippingMethodRepository = shippingMethodRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _discountRepository = discountRepository;
        _inventoryService = inventoryService;
        _paymentService = paymentService;
        _walletRepository = walletRepository;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _orderDomainService = orderDomainService;
        _inventoryReservationService = inventoryReservationService;
        _logger = logger;
    }

    public async Task<ServiceResult<CheckoutResultDto>> Handle(
        CheckoutFromCartCommand request,
        CancellationToken ct)
    {
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
                var cart = await _cartRepository.GetByUserIdAsync(request.UserId, ct);
                if (cart == null || !cart.CartItems.Any())
                    return ServiceResult<CheckoutResultDto>.Failure("سبد خرید خالی است.");

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

                var shippingMethod = await _shippingMethodRepository.GetByIdAsync(request.ShippingId, ct);
                if (shippingMethod == null || !shippingMethod.IsActive)
                    return ServiceResult<CheckoutResultDto>.Failure("روش ارسال انتخاب شده معتبر نیست.");

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

                var stockValidation = _inventoryReservationService.ValidateBatchAvailability(variantItems);
                if (!stockValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(stockValidation.GetErrorsSummary());

                var itemsValidation = _orderDomainService.ValidateOrderItems(orderItemSnapshots);
                if (!itemsValidation.IsValid)
                    return ServiceResult<CheckoutResultDto>.Failure(itemsValidation.GetErrorsSummary());

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

                if (!string.IsNullOrWhiteSpace(request.DiscountCode))
                {
                    var discountCode = await _discountRepository.GetByCodeAsync(request.DiscountCode, ct);
                    if (discountCode == null)
                        return ServiceResult<CheckoutResultDto>.Failure("کد تخفیف نامعتبر است.");

                    var userUsageCount = await _discountRepository.CountUserUsageAsync(
                        discountCode.Id, request.UserId, ct);

                    var validation = discountCode.ValidateForApplication(
                        order.TotalAmount.Amount,
                        request.UserId,
                        userUsageCount);

                    if (!validation.IsValid)
                        return ServiceResult<CheckoutResultDto>.Failure(validation.Error!);

                    var discountMoney = discountCode.CalculateDiscountMoney(order.TotalAmount);
                    discountCode.RecordUsage(request.UserId, order.Id, discountMoney);
                    order.ApplyDiscount(discountCode.Id, discountMoney);

                    _discountRepository.Update(discountCode);
                    await _orderRepository.UpdateAsync(order, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                if (request.GatewayName.Equals("Wallet", StringComparison.OrdinalIgnoreCase))
                {
                    var wallet = await _walletRepository.GetByUserIdAsync(request.UserId, ct);
                    if (wallet == null || wallet.AvailableBalance < order.FinalAmount.Amount)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult<CheckoutResultDto>.Failure("موجودی کیف پول شما کافی نیست.");
                    }

                    var debitCommand = new Application.Wallet.Features.Commands.DebitWallet.DebitWalletCommand(
                        UserId: request.UserId,
                        Amount: order.FinalAmount.Amount,
                        TransactionType: WalletTransactionType.OrderPayment,
                        ReferenceType: WalletReferenceType.Order,
                        ReferenceId: order.Id,
                        IdempotencyKey: $"order-pay-{order.Id}-{request.IdempotencyKey}"
                    );

                    var debitResult = await _mediator.Send(debitCommand, ct);
                    if (debitResult.IsFailed)
                    {
                        await _unitOfWork.RollbackTransactionAsync(ct);
                        return ServiceResult<CheckoutResultDto>.Failure(debitResult.Error ?? "خطا در کسر از کیف پول.");
                    }

                    order.MarkAsPaid(refId: 0, cardPan: "Wallet");
                    order.StartProcessing();

                    await _orderRepository.UpdateAsync(order, ct);
                    await _cartRepository.ClearCartAsync(request.UserId, ct);
                    await _unitOfWork.SaveChangesAsync(ct);
                    await _unitOfWork.CommitTransactionAsync(ct);

                    _logger.LogInformation(
                        "Order {OrderId} paid via Wallet for user {UserId}.",
                        order.Id, request.UserId);

                    return ServiceResult<CheckoutResultDto>.Success(new CheckoutResultDto
                    {
                        OrderId = order.Id,
                        Success = true,
                    });
                }
                else
                {
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