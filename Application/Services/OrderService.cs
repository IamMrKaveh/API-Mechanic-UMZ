namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderService> _logger;
    private readonly IAuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ICartRepository _cartRepository;
    private readonly IDiscountService _discountService;
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly FrontendUrlsDto _frontendUrls;
    private readonly IPaymentService _paymentService;

    public OrderService(
        IOrderRepository orderRepository,
        IOrderStatusRepository orderStatusRepository,
        IPaymentGateway paymentGateway,
        IUnitOfWork unitOfWork,
        ILogger<OrderService> logger,
        IAuditService auditService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IInventoryService inventoryService,
        ICartRepository cartRepository,
        IDiscountService discountService,
        IShippingMethodRepository shippingMethodRepository,
        IOptions<FrontendUrlsDto> frontendUrls,
        IPaymentService paymentService)
    {
        _orderRepository = orderRepository;
        _orderStatusRepository = orderStatusRepository;
        _paymentGateway = paymentGateway;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _auditService = auditService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _inventoryService = inventoryService;
        _cartRepository = cartRepository;
        _discountService = discountService;
        _shippingMethodRepository = shippingMethodRepository;
        _frontendUrls = frontendUrls.Value;
        _paymentService = paymentService;
    }

    public async Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? userId, bool includeDetails, int? filterUserId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var (orders, totalItems) = await _orderRepository.GetOrdersAsync(userId, includeDetails, filterUserId, statusId, fromDate, toDate, page, pageSize);
        var orderDtos = orders.Select(o => new { o.Id, o.TotalAmount, o.FinalAmount, o.CreatedAt, OrderStatus = new { o.OrderStatus.Id, o.OrderStatus.Name, o.OrderStatus.Icon }, ItemsCount = o.OrderItems.Count });
        return (orderDtos, totalItems);
    }

    public async Task<object?> GetOrderByIdAsync(int orderId, int? userId, bool isAdmin)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, userId, true);
        if (order == null) return null;
        return new
        {
            order.Id,
            order.TotalAmount,
            order.ShippingCost,
            order.DiscountAmount,
            order.FinalAmount,
            order.CreatedAt,
            order.DeliveryDate,
            OrderStatus = new { order.OrderStatus.Id, order.OrderStatus.Name, order.OrderStatus.Icon },
            AddressSnapshot = JsonSerializer.Deserialize<UserAddressDto>(order.AddressSnapshot),
            OrderItems = order.OrderItems.Select(oi => new { oi.Id, oi.Quantity, oi.SellingPrice, oi.Amount, Product = new { oi.Variant.Product.Name, Icon = oi.Variant.Product.Images.FirstOrDefault(i => i.IsPrimary)?.FilePath } })
        };
    }

    public async Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto dto, int userId, string idempotencyKey)
    {
        var ipAddress = "IP_FROM_CONTROLLER";

        if (await _orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey)) throw new InvalidOperationException("Duplicate checkout request.");

        var cart = await _cartRepository.GetCartAsync(userId);
        if (cart == null || !cart.CartItems.Any()) return new CheckoutFromCartResultDto { Error = "Cart is empty" };

        string orderReference = "";

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userAddress = await _userRepository.GetUserAddressAsync(dto.UserAddressId ?? 0); // Simplify
                if (userAddress == null && dto.NewAddress != null) { /* Create address logic */ }

                var order = new Order { UserId = userId, CreatedAt = DateTime.UtcNow, IdempotencyKey = idempotencyKey, ReceiverName = "Temp", AddressSnapshot = "{}" };
                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();
                orderReference = $"ORDER-{order.Id}";

                foreach (var item in cart.CartItems)
                {
                    await _inventoryService.LogTransactionAsync(item.VariantId, "Reservation", -item.Quantity, null, userId, "Order Reserve", orderReference, null, false);
                }
                await _unitOfWork.SaveChangesAsync();

                var paymentResult = await _paymentService.InitiatePaymentAsync(order.Id, userId, order.FinalAmount, $"Order {order.Id}", null, null, "ZarinPal", ipAddress);

                if (paymentResult.Authority != null)
                {
                    _cartRepository.RemoveCart(cart);
                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return new CheckoutFromCartResultDto { OrderId = order.Id, PaymentUrl = paymentResult.PaymentUrl, Authority = paymentResult.Authority };
                }

                throw new Exception(paymentResult.Error);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Checkout failed");
                return new CheckoutFromCartResultDto { Error = ex.Message };
            }
        });
    }

    public async Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        return await _paymentService.VerifyPaymentAsync(authority, status);
    }
}