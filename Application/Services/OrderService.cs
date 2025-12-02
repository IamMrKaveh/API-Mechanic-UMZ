namespace Application.Services;

public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICartRepository _cartRepository;
    private readonly IUserRepository _userRepository;
    private readonly IDiscountService _discountService;
    private readonly IPaymentService _paymentService;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;
    private readonly LedkaContext _context;
    private readonly IOptions<FrontendUrlsDto> _frontendUrls;

    public OrderService(
        IOrderRepository orderRepository,
        ICartRepository cartRepository,
        IUserRepository userRepository,
        IDiscountService discountService,
        IPaymentService paymentService,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<OrderService> logger,
        LedkaContext context,
        IOptions<FrontendUrlsDto> frontendUrls)
    {
        _orderRepository = orderRepository;
        _cartRepository = cartRepository;
        _userRepository = userRepository;
        _discountService = discountService;
        _paymentService = paymentService;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
        _context = context;
        _frontendUrls = frontendUrls;
    }

    public async Task<(IEnumerable<OrderDto> Orders, int TotalItems)> GetOrdersAsync(int? userId, bool includeDeleted, int? currentUserId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var (orders, total) = await _orderRepository.GetOrdersAsync(userId, includeDeleted, currentUserId, statusId, fromDate, toDate, page, pageSize);
        return (_mapper.Map<IEnumerable<OrderDto>>(orders), total);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(int id, int? userId, bool isAdmin)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id, userId, isAdmin);
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto dto, int userId, string idempotencyKey)
    {
        var existingOrder = await _context.Orders
            .Include(o => o.PaymentTransactions)
            .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);

        if (existingOrder != null)
        {
            if (existingOrder.IsPaid)
            {
                return new CheckoutFromCartResultDto { Error = "This order has already been paid." };
            }

            var lastTransaction = existingOrder.PaymentTransactions
                .OrderByDescending(t => t.CreatedAt)
                .FirstOrDefault();

            if (lastTransaction != null && lastTransaction.Status == "Pending")
            {
                return new CheckoutFromCartResultDto
                {
                    OrderId = existingOrder.Id,
                    Authority = lastTransaction.Authority,
                    PaymentUrl = $"https://payment.zarinpal.com/pg/StartPay/{lastTransaction.Authority}"
                };
            }

            return new CheckoutFromCartResultDto
            {
                OrderId = existingOrder.Id,
                Error = "Order exists but payment link expired or invalid. Please retry payment from order history."
            };
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var cart = await _cartRepository.GetByUserIdAsync(userId);
                if (cart == null || !cart.CartItems.Any())
                {
                    return new CheckoutFromCartResultDto { Error = "Cart is empty." };
                }

                var variantIds = cart.CartItems.Select(c => c.VariantId).ToList();
                var variants = await _context.ProductVariants
                    .Include(v => v.Product)
                    .Where(v => variantIds.Contains(v.Id))
                    .ToListAsync();

                foreach (var item in cart.CartItems)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == item.VariantId);
                    if (variant == null) return new CheckoutFromCartResultDto { Error = $"Product variant {item.VariantId} not found." };

                    if (dto.ExpectedItems != null)
                    {
                        var expected = dto.ExpectedItems.FirstOrDefault(e => e.VariantId == item.VariantId);
                        if (expected != null && expected.Price != variant.SellingPrice)
                        {
                            return new CheckoutFromCartResultDto { Error = "Prices have changed. Please refresh your cart." };
                        }
                    }

                    if (!variant.IsUnlimited && variant.Stock < item.Quantity)
                    {
                        return new CheckoutFromCartResultDto { Error = $"Insufficient stock for {variant.Product.Name}" };
                    }
                }

                UserAddress addressToUse;
                if (dto.UserAddressId.HasValue)
                {
                    addressToUse = await _userRepository.GetUserAddressAsync(dto.UserAddressId.Value);
                    if (addressToUse == null || addressToUse.UserId != userId)
                    {
                        return new CheckoutFromCartResultDto { Error = "Invalid user address." };
                    }
                }
                else if (dto.NewAddress != null)
                {
                    addressToUse = _mapper.Map<UserAddress>(dto.NewAddress);
                    addressToUse.UserId = userId;

                    if (dto.SaveNewAddress)
                    {
                        await _userRepository.AddAddressAsync(addressToUse);
                        await _unitOfWork.SaveChangesAsync();
                    }
                }
                else
                {
                    return new CheckoutFromCartResultDto { Error = "Delivery address is required." };
                }

                var shippingMethod = await _context.ShippingMethods.FindAsync(dto.ShippingMethodId);
                if (shippingMethod == null) return new CheckoutFromCartResultDto { Error = "Invalid shipping method." };

                decimal totalAmount = 0;
                decimal totalProfit = 0;
                var orderItems = new List<OrderItem>();

                foreach (var item in cart.CartItems)
                {
                    var variant = variants.First(v => v.Id == item.VariantId);
                    var amount = variant.SellingPrice * item.Quantity;
                    var profit = (variant.SellingPrice - variant.PurchasePrice) * item.Quantity;

                    totalAmount += amount;
                    totalProfit += profit;

                    orderItems.Add(new OrderItem
                    {
                        VariantId = item.VariantId,
                        Quantity = item.Quantity,
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

                var order = new Order
                {
                    UserId = userId,
                    UserAddressId = dto.SaveNewAddress ? addressToUse.Id : null,
                    ReceiverName = addressToUse.ReceiverName,
                    AddressSnapshot = JsonSerializer.Serialize(_mapper.Map<UserAddressDto>(addressToUse)),
                    TotalAmount = totalAmount,
                    TotalProfit = totalProfit,
                    ShippingCost = shippingMethod.Cost,
                    DiscountAmount = discountAmount,
                    FinalAmount = totalAmount + shippingMethod.Cost - discountAmount,
                    OrderStatusId = 1,
                    ShippingMethodId = dto.ShippingMethodId,
                    DiscountCodeId = discountCodeId,
                    IdempotencyKey = idempotencyKey,
                    OrderItems = orderItems
                };

                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                foreach (var item in orderItems)
                {
                    await _inventoryService.LogTransactionAsync(
                        item.VariantId, "Sale", -item.Quantity, item.Id, userId, "Order placed", $"ORDER-{order.Id}", null, false);
                }

                await _cartRepository.ClearCartAsync(userId);

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                var paymentInitiationDto = new PaymentInitiationDto
                {
                    Amount = order.FinalAmount,
                    Description = $"Order #{order.Id}",
                    CallbackUrl = $"{_frontendUrls.Value.BaseUrl}/payment/callback",
                    Mobile = await _context.Users.Where(u => u.Id == userId).Select(u => u.PhoneNumber).FirstOrDefaultAsync(),
                    OrderId = order.Id,
                    UserId = userId
                };

                var paymentResult = await _paymentService.InitiatePaymentAsync(paymentInitiationDto);

                if (!paymentResult.IsSuccess)
                {
                    return new CheckoutFromCartResultDto { Error = "Failed to initiate payment: " + paymentResult.Message };
                }

                return new CheckoutFromCartResultDto
                {
                    OrderId = order.Id,
                    PaymentUrl = paymentResult.PaymentUrl,
                    Authority = paymentResult.Authority
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync();
                return new CheckoutFromCartResultDto { Error = "Inventory or price changed during checkout. Please try again." };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Checkout failed");
                return new CheckoutFromCartResultDto { Error = "An unexpected error occurred." };
            }
        });
    }

    public async Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status)
    {
        var result = await _paymentService.VerifyPaymentAsync(authority, status);
        return result;
    }
}