namespace Application.Order.Features.Commands.CreateOrder;

public class AdminCreateOrderHandler : IRequestHandler<CreateOrderCommand, ServiceResult<int>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IUserRepository _userRepository;
    private readonly IShippingRepository _shippingRepository;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderDomainService _orderDomainService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminCreateOrderHandler> _logger;

    public AdminCreateOrderHandler(
        IOrderRepository orderRepository,
        IUserRepository userRepository,
        IShippingRepository shippingRepository,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        OrderDomainService orderDomainService,
        IAuditService auditService,
        ILogger<AdminCreateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _userRepository = userRepository;
        _shippingRepository = shippingRepository;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _orderDomainService = orderDomainService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ServiceResult<int>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken))
            return ServiceResult<int>.Failure("درخواست تکراری. سفارش قبلاً ثبت شده است.");

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                // 1. Validate Address
                var userAddress = await _userRepository.GetUserAddressAsync(
                    request.Dto.UserAddressId, cancellationToken);
                if (userAddress == null || userAddress.UserId != request.Dto.UserId)
                    return ServiceResult<int>.Failure("آدرس کاربر نامعتبر است.");

                // 2. Validate Shipping
                var shipping = await _shippingRepository.GetByIdAsync(
                    request.Dto.ShippingId, cancellationToken);
                if (shipping == null || !shipping.IsActive)
                    return ServiceResult<int>.Failure("روش ارسال انتخاب شده معتبر نیست.");

                // 3. Build OrderItemSnapshots
                var orderItemSnapshots = new List<OrderItemSnapshot>();

                foreach (var itemDto in request.Dto.OrderItems)
                {
                    orderItemSnapshots.Add(OrderItemSnapshot.Create(
                        variantId: itemDto.VariantId,
                        productId: 0,
                        productName: "محصول",
                        variantSku: null,
                        variantAttributes: null,
                        quantity: itemDto.Quantity,
                        purchasePrice: 0,
                        sellingPrice: itemDto.SellingPrice));
                }

                // 4. Apply Discount
                DiscountApplicationResult? discountResult = null;
                if (!string.IsNullOrEmpty(request.Dto.DiscountCode))
                {
                    var totalAmount = orderItemSnapshots.Sum(x => x.SellingPrice.Amount * x.Quantity);
                    var discountServiceResult = await _discountService.ValidateAndApplyDiscountAsync(
                        request.Dto.DiscountCode, totalAmount, request.Dto.UserId);

                    if (discountServiceResult.IsSucceed && discountServiceResult.Data != null)
                    {
                        discountResult = DiscountApplicationResult.Success(
                            discountServiceResult.Data.DiscountCodeId,
                            Money.FromDecimal(discountServiceResult.Data.DiscountAmount));
                    }
                }

                // 5. Create Order using Domain Service
                var order = _orderDomainService.PlaceOrder(
                    request.Dto.UserId,
                    userAddress,
                    request.Dto.ReceiverName,
                    shipping,
                    request.IdempotencyKey,
                    orderItemSnapshots,
                    discountResult);

                await _orderRepository.AddAsync(order, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // 6. Reserve Inventory
                foreach (var oi in order.OrderItems)
                {
                    await _inventoryService.LogTransactionAsync(
                        oi.VariantId,
                        "Sale",
                        -oi.Quantity,
                        oi.Id,
                        order.UserId,
                        "Admin Created Order",
                        $"ORDER-{order.Id}",
                        null,
                        false,
                        cancellationToken);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _auditService.LogOrderEventAsync(
                    order.Id,
                    "AdminCreateOrder",
                    request.AdminUserId,
                    $"سفارش توسط مدیر ایجاد شد. شماره سفارش: {order.OrderNumber.Value}");

                _logger.LogInformation(
                    "Admin {AdminId} created order {OrderId} for user {UserId}",
                    request.AdminUserId, order.Id, request.Dto.UserId);

                return ServiceResult<int>.Success(order.Id);
            }
            catch (DomainException ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ServiceResult<int>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error creating admin order");
                return ServiceResult<int>.Failure("خطایی در ایجاد سفارش رخ داد.");
            }
        }, cancellationToken);
    }
}