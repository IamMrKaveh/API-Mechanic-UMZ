namespace Application.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<AdminOrderService> _logger;
    private readonly IAuditService _auditService;
    private readonly IDiscountService _discountService;
    private readonly IInventoryService _inventoryService;
    private readonly IMediaService _mediaService;
    private readonly INotificationService _notificationService;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminOrderService(
        IOrderRepository orderRepository,
        ILogger<AdminOrderService> logger,
        IAuditService auditService,
        IDiscountService discountService,
        IInventoryService inventoryService,
        IMediaService mediaService,
        INotificationService notificationService,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _logger = logger;
        _auditService = auditService;
        _discountService = discountService;
        _inventoryService = inventoryService;
        _mediaService = mediaService;
        _notificationService = notificationService;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
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
            o.RowVersion,
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
                            a.AttributeValue.HexCode
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
            orderData.RowVersion,
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


    public async Task<Domain.Order.Order> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey)
    {
        var userAddress = await _userRepository.GetUserAddressAsync(orderDto.UserAddressId);
        if (userAddress == null || userAddress.UserId != orderDto.UserId)
            throw new ArgumentException("Invalid user address");

        var variantIds = orderDto.OrderItems.Select(i => i.VariantId).ToList();
        var variants = await _orderRepository.GetVariantsByIdsAsync(variantIds);

        decimal totalAmount = 0;
        decimal totalProfit = 0;
        var orderItems = new List<Domain.Order.OrderItem>();

        foreach (var itemDto in orderDto.OrderItems)
        {
            if (!variants.TryGetValue(itemDto.VariantId, out var variant))
                throw new ArgumentException($"Product variant {itemDto.VariantId} not found");

            if (!variant.IsUnlimited && variant.Stock < itemDto.Quantity)
                throw new InvalidOperationException($"Not enough stock for product {variant.Product.Name}.");

            await _inventoryService.LogTransactionAsync(variant.Id, "Sale", -itemDto.Quantity, null, orderDto.UserId, $"Order creation", null, variant.RowVersion);

            var amount = itemDto.SellingPrice * itemDto.Quantity;
            var profit = (itemDto.SellingPrice - variant.PurchasePrice) * itemDto.Quantity;

            totalAmount += amount;
            totalProfit += profit;

            orderItems.Add(new Domain.Order.OrderItem
            {
                VariantId = variant.Id,
                PurchasePrice = variant.PurchasePrice,
                SellingPrice = itemDto.SellingPrice,
                Quantity = itemDto.Quantity,
                Amount = amount,
                Profit = profit
            });
        }

        var shippingMethod = await _orderRepository.GetShippingMethodAsync(orderDto.ShippingMethodId);
        if (shippingMethod == null) throw new ArgumentException("Invalid shipping method");

        decimal discountAmount = 0;
        int? discountId = null;
        if (!string.IsNullOrEmpty(orderDto.DiscountCode))
        {
            var (discount, error) = await _discountService.ValidateAndGetDiscountAsync(orderDto.DiscountCode, orderDto.UserId, totalAmount);
            if (error != null) throw new ArgumentException(error);

            if (discount != null)
            {
                discountAmount = (totalAmount * discount.Percentage) / 100;
                if (discount.MaxDiscountAmount.HasValue && discountAmount > discount.MaxDiscountAmount.Value)
                {
                    discountAmount = discount.MaxDiscountAmount.Value;
                }
                discountId = discount.Id;
                discount.UsedCount++;
            }
        }

        var order = new Domain.Order.Order
        {
            UserId = orderDto.UserId,
            AddressSnapshot = JsonSerializer.Serialize(userAddress),
            UserAddressId = userAddress.Id,
            CreatedAt = DateTime.UtcNow,
            OrderStatusId = orderDto.OrderStatusId,
            IdempotencyKey = idempotencyKey,
            ShippingMethodId = shippingMethod.Id,
            ShippingCost = shippingMethod.Cost,
            DiscountAmount = discountAmount,
            DiscountCodeId = discountId,
            TotalAmount = totalAmount,
            TotalProfit = totalProfit,
            FinalAmount = totalAmount + shippingMethod.Cost - discountAmount,
            OrderItems = orderItems
        };

        await _orderRepository.AddOrderAsync(order);

        if (discountId.HasValue)
        {
            await _orderRepository.AddDiscountUsageAsync(new Domain.Discount.DiscountUsage
            {
                UserId = orderDto.UserId,
                DiscountCodeId = discountId.Value,
                OrderId = order.Id,
                DiscountAmount = discountAmount,
                UsedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.SaveChangesAsync();
        return order;
    }

    public async Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(orderId);
        if (order == null) return false;

        if (orderDto.RowVersion != null)
            _orderRepository.SetOrderRowVersion(order, orderDto.RowVersion);
        else
            throw new ArgumentException("RowVersion is required for concurrency control.");

        if (orderDto.OrderStatusId.HasValue)
        {
            if (!await _orderRepository.OrderStatusExistsAsync(orderDto.OrderStatusId.Value))
                throw new ArgumentException("Invalid order status ID");
            order.OrderStatusId = orderDto.OrderStatusId.Value;
        }

        if (orderDto.ShippingMethodId.HasValue)
        {
            var shippingMethod = await _orderRepository.GetShippingMethodAsync(orderDto.ShippingMethodId.Value);
            if (shippingMethod == null) throw new ArgumentException("Invalid shipping method ID");
            order.ShippingMethodId = shippingMethod.Id;
            order.ShippingCost = shippingMethod.Cost;
        }

        if (orderDto.UserAddressId.HasValue)
        {
            var userAddress = await _userRepository.GetUserAddressAsync(orderDto.UserAddressId.Value);
            if (userAddress == null || userAddress.UserId != order.UserId)
                throw new ArgumentException("Invalid user address ID");
            order.UserAddressId = userAddress.Id;
            order.AddressSnapshot = JsonSerializer.Serialize(userAddress);
        }

        if (orderDto.DeliveryDate.HasValue)
            order.DeliveryDate = orderDto.DeliveryDate;

        order.FinalAmount = order.TotalAmount + order.ShippingCost - order.DiscountAmount;
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusByIdDto statusDto)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(id);
        if (order == null) return false;

        _orderRepository.SetOrderRowVersion(order, statusDto.RowVersion);

        if (!await _orderRepository.OrderStatusExistsAsync(statusDto.OrderStatusId))
            throw new ArgumentException("Invalid Order Status ID");

        order.OrderStatusId = statusDto.OrderStatusId;

        var statusName = await _orderRepository.GetOrderStatusNameAsync(statusDto.OrderStatusId);
        await _auditService.LogOrderEventAsync(id, "UpdateStatus", order.UserId, $"Order status changed to {statusName}");

        await _notificationService.CreateNotificationAsync(
            order.UserId,
            "تغییر وضعیت سفارش",
            $"وضعیت سفارش شما با شماره {order.Id} به '{statusName}' تغییر کرد.",
            "OrderStatusChanged",
            $"/dashboard/order/{order.Id}"
        );

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderAsync(int orderId)
    {
        var order = await _orderRepository.GetOrderWithItemsAsync(orderId);
        if (order == null) return false;

        const int shippedStatusId = 3;
        if (order.OrderStatusId >= shippedStatusId)
            throw new InvalidOperationException("Cannot delete an order that has been shipped or delivered. Consider changing its status instead.");

        foreach (var orderItem in order.OrderItems)
        {
            await _inventoryService.LogTransactionAsync(orderItem.VariantId, "Return", orderItem.Quantity, orderItem.Id, order.UserId, $"Order Deletion {order.Id}", null, orderItem.Variant.RowVersion);
        }

        _orderRepository.DeleteOrder(order);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        return await _orderRepository.GetOrderStatisticsAsync(fromDate, toDate);
    }
}