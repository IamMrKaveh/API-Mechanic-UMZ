namespace Application.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderStatusRepository _orderStatusRepository;
    private readonly ILogger<AdminOrderService> _logger;
    private readonly IAuditService _auditService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IServiceProvider _serviceProvider;


    public AdminOrderService(
        IOrderRepository orderRepository,
        IOrderStatusRepository orderStatusRepository,
        ILogger<AdminOrderService> logger,
        IAuditService auditService,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IMediaService mediaService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IServiceProvider serviceProvider)
    {
        _orderRepository = orderRepository;
        _orderStatusRepository = orderStatusRepository;
        _logger = logger;
        _auditService = auditService;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _serviceProvider = serviceProvider;
    }

    public async Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
    {
        var (orders, totalItems) = await _orderRepository.GetOrdersAsync(null, true, userId, statusId, fromDate, toDate, page, pageSize);

        var orderDtos = orders.Select(o => new
        {
            o.Id,
            AddressSnapshot = JsonSerializer.Deserialize<UserAddressDto>(o.AddressSnapshot, (JsonSerializerOptions?)null),
            o.TotalAmount,
            o.TotalProfit,
            o.ShippingCost,
            o.DiscountAmount,
            o.FinalAmount,
            o.CreatedAt,
            RowVersion = o.RowVersion != null ? Convert.ToBase64String(o.RowVersion) : null,
            o.OrderStatusId,
            User = o.User != null ? new
            {
                o.User.Id,
                o.User.PhoneNumber,
                o.User.FirstName,
                o.User.LastName
            } : null,
            OrderStatus = o.OrderStatus != null ? new
            {
                o.OrderStatus.Id,
                o.OrderStatus.Name,
                o.OrderStatus.Icon
            } : null,
            ShippingMethod = o.ShippingMethod != null ? new
            {
                o.ShippingMethod.Id,
                o.ShippingMethod.Name,
                o.ShippingMethod.Cost
            } : null,
            OrderItemsCount = o.OrderItems.Count
        });

        return (orderDtos, totalItems);
    }

    public async Task<object?> GetOrderByIdAsync(int orderId)
    {
        var orderData = await _orderRepository.GetOrderByIdAsync(orderId, null, true);

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
                PurchasePrice = (decimal?)oi.PurchasePrice,
                oi.SellingPrice,
                oi.Quantity,
                oi.Amount,
                Profit = (decimal?)oi.Profit,
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
            orderData.TotalProfit,
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

    public async Task<ServiceResult> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusByIdDto dto)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, null, true);
        if (order == null)
        {
            return ServiceResult.Fail("Order not found.");
        }

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(dto.RowVersion));
        }

        var newStatus = await _orderStatusRepository.GetByIdAsync(dto.OrderStatusId);
        if (newStatus == null)
        {
            return ServiceResult.Fail("Order status not found.");
        }

        var oldStatusName = order.OrderStatus?.Name ?? "Unknown";
        order.OrderStatusId = dto.OrderStatusId;
        order.UpdatedAt = DateTime.UtcNow;

        _orderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                order.UserId,
                "تغییر وضعیت سفارش",
                $"وضعیت سفارش #{order.Id} از {oldStatusName} به {newStatus.Name} تغییر کرد.",
                "OrderStatus",
                $"/dashboard/order/{order.Id}",
                order.Id,
                "Order"
            );

            await _auditService.LogOrderEventAsync(
                order.Id,
                "UpdateOrderStatus",
                order.UserId,
                $"Order {order.Id} status changed from {oldStatusName} to {newStatus.Name}"
            );

            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This order was modified by another user.  Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> UpdateOrderAsync(int orderId, UpdateOrderDto dto)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, null, true);
        if (order == null)
        {
            return ServiceResult.Fail("Order not found.");
        }

        if (!string.IsNullOrEmpty(dto.RowVersion))
        {
            _orderRepository.SetOriginalRowVersion(order, Convert.FromBase64String(dto.RowVersion));
        }

        if (dto.OrderStatusId.HasValue)
        {
            order.OrderStatusId = dto.OrderStatusId.Value;
        }

        if (dto.ShippingMethodId.HasValue)
        {
            order.ShippingMethodId = dto.ShippingMethodId.Value;
        }

        if (dto.DeliveryDate.HasValue)
        {
            order.DeliveryDate = dto.DeliveryDate.Value;
        }

        order.UpdatedAt = DateTime.UtcNow;
        _orderRepository.Update(order);

        try
        {
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult.Ok();
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult.Fail("This order was modified by another user.   Please refresh and try again.");
        }
    }

    public async Task<ServiceResult> DeleteOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetOrderByIdAsync(orderId, null, true);
        if (order == null)
        {
            return ServiceResult.Fail("Order not found.");
        }

        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        _orderRepository.Update(order);

        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogOrderEventAsync(
            order.Id,
            "DeleteOrder",
            order.UserId,
            $"Order {order.Id} was deleted"
        );

        return ServiceResult.Ok();
    }

    public async Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var statistics = await _orderRepository.GetOrderStatisticsAsync(fromDate, toDate);
        var statusStatistics = await _orderRepository.GetOrderStatusStatisticsAsync(fromDate, toDate);

        return new
        {
            GeneralStatistics = statistics,
            StatusStatistics = statusStatistics
        };
    }

    public async Task<Order> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey)
    {
        if (await _orderRepository.ExistsByIdempotencyKeyAsync(idempotencyKey))
        {
            throw new InvalidOperationException("Duplicate order request.");
        }

        return await _unitOfWork.ExecuteStrategyAsync(async () =>
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var userAddress = await _userRepository.GetUserAddressAsync(orderDto.UserAddressId);
                if (userAddress == null || userAddress.UserId != orderDto.UserId)
                    throw new ArgumentException("Invalid user address");

                var variantIds = orderDto.OrderItems.Select(i => i.VariantId).ToList();
                // Locking variants
                var variants = await _orderRepository.GetVariantsByIdsForUpdateAsync(variantIds);

                decimal totalAmount = 0;
                decimal totalProfit = 0;
                var orderItems = new List<OrderItem>();

                foreach (var itemDto in orderDto.OrderItems)
                {
                    var variant = variants.FirstOrDefault(v => v.Id == itemDto.VariantId);
                    if (variant == null)
                        throw new ArgumentException($"Variant {itemDto.VariantId} not found");

                    if (!variant.IsUnlimited && variant.Stock < itemDto.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for {variant.Product.Name}");

                    var amount = itemDto.SellingPrice * itemDto.Quantity;
                    var profit = (itemDto.SellingPrice - variant.PurchasePrice) * itemDto.Quantity;

                    totalAmount += amount;
                    totalProfit += profit;

                    orderItems.Add(new OrderItem
                    {
                        VariantId = itemDto.VariantId,
                        Quantity = itemDto.Quantity,
                        SellingPrice = itemDto.SellingPrice,
                        PurchasePrice = variant.PurchasePrice,
                        Amount = amount,
                        Profit = profit
                    });
                }

                decimal discountAmount = 0;
                int? discountCodeId = null;

                if (!string.IsNullOrEmpty(orderDto.DiscountCode))
                {
                    var discountResult = await _discountService.ValidateAndApplyDiscountAsync(orderDto.DiscountCode, totalAmount, orderDto.UserId);
                    if (discountResult.Success && discountResult.Data != null)
                    {
                        discountAmount = discountResult.Data.DiscountAmount;
                        discountCodeId = discountResult.Data.DiscountCodeId;
                    }
                }

                var shippingMethod = await _orderRepository.GetShippingMethodByIdAsync(orderDto.ShippingMethodId);
                var shippingCost = shippingMethod?.Cost ?? 0;

                var order = new Order
                {
                    UserId = orderDto.UserId,
                    UserAddressId = orderDto.UserAddressId,
                    ReceiverName = orderDto.ReceiverName,
                    AddressSnapshot = JsonSerializer.Serialize(new UserAddressDto
                    {
                        Id = userAddress.Id,
                        Title = userAddress.Title,
                        ReceiverName = userAddress.ReceiverName,
                        PhoneNumber = userAddress.PhoneNumber,
                        Province = userAddress.Province,
                        City = userAddress.City,
                        Address = userAddress.Address,
                        PostalCode = userAddress.PostalCode,
                        IsDefault = userAddress.IsDefault
                    }),
                    TotalAmount = totalAmount,
                    TotalProfit = totalProfit,
                    ShippingCost = shippingCost,
                    DiscountAmount = discountAmount,
                    FinalAmount = totalAmount + shippingCost - discountAmount,
                    OrderStatusId = orderDto.OrderStatusId,
                    ShippingMethodId = orderDto.ShippingMethodId,
                    DiscountCodeId = discountCodeId,
                    IdempotencyKey = idempotencyKey,
                    OrderItems = orderItems
                };

                await _orderRepository.AddAsync(order);
                await _unitOfWork.SaveChangesAsync();

                // Inventory
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
                       false
                   );
                }

                await _unitOfWork.SaveChangesAsync();
                await transaction.CommitAsync();

                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }
}