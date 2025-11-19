namespace Application.Services;

public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderItemService> _logger;
    private readonly IMediaService _mediaService;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;

    public OrderItemService(
        IOrderItemRepository orderItemRepository,
        IOrderRepository orderRepository,
        ILogger<OrderItemService> logger,
        IMediaService mediaService,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork)
    {
        _orderItemRepository = orderItemRepository;
        _orderRepository = orderRepository;
        _logger = logger;
        _mediaService = mediaService;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
    }

    public async Task<(IEnumerable<object> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize)
    {
        var (items, total) = await _orderItemRepository.GetOrderItemsAsync(currentUserId, isAdmin, orderId, page, pageSize);

        var dtos = items.Select(oi => new
        {
            oi.Id,
            oi.OrderId,
            oi.VariantId,
            ProductName = oi.Variant.Product != null ? oi.Variant.Product.Name : "N/A",
            PurchasePrice = isAdmin ? (decimal?)oi.PurchasePrice : null,
            oi.SellingPrice,
            oi.Quantity,
            oi.Amount,
            Profit = isAdmin ? (decimal?)oi.Profit : null,
        });

        return (dtos, total);
    }

    public async Task<object?> GetOrderItemByIdAsync(int orderItemId, int? currentUserId, bool isAdmin)
    {
        var item = await _orderItemRepository.GetOrderItemByIdAsync(orderItemId);

        if (item == null) return null;

        if (!isAdmin && (item.Order == null || item.Order.UserId != currentUserId))
        {
            _logger.LogWarning("Unauthorized access attempt for OrderItem {OrderItemId} by User {UserId}", orderItemId, currentUserId);
            return null;
        }

        var icon = await _mediaService.GetPrimaryImageUrlAsync("Product", item.Variant.ProductId);
        return new
        {
            item.Id,
            item.OrderId,
            item.VariantId,
            Product = item.Variant.Product != null ? new
            {
                item.Variant.Product.Id,
                item.Variant.Product.Name,
                Icon = icon,
                Category = item.Variant.Product.CategoryGroup?.Category != null ? new { item.Variant.Product.CategoryGroup.Category.Id, item.Variant.Product.CategoryGroup.Category.Name } : null
            } : null,
            PurchasePrice = isAdmin ? (decimal?)item.PurchasePrice : null,
            item.SellingPrice,
            item.Quantity,
            item.Amount,
            Profit = isAdmin ? (decimal?)item.Profit : null,
            RowVersion = item.RowVersion != null ? Convert.ToBase64String(item.RowVersion) : null
        };
    }

    public async Task<Domain.Order.OrderItem> CreateOrderItemAsync(CreateOrderItemDto itemDto)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(itemDto.OrderId);
        if (order == null) throw new KeyNotFoundException("Order not found");

        var variant = await _orderItemRepository.GetProductVariantWithProductAsync(itemDto.VariantId);
        if (variant?.Product == null) throw new KeyNotFoundException("Product variant not found");

        await _inventoryService.LogTransactionAsync(variant.Id, "Sale", -itemDto.Quantity, null, order.UserId, $"Added to order {order.Id}");

        var amount = itemDto.SellingPrice * itemDto.Quantity;
        var profit = (itemDto.SellingPrice - variant.PurchasePrice) * itemDto.Quantity;

        var newOrderItem = new Domain.Order.OrderItem
        {
            OrderId = itemDto.OrderId,
            VariantId = itemDto.VariantId,
            Quantity = itemDto.Quantity,
            SellingPrice = itemDto.SellingPrice,
            PurchasePrice = variant.PurchasePrice,
            Amount = amount,
            Profit = profit
        };

        await _orderItemRepository.AddOrderItemAsync(newOrderItem);

        order.TotalAmount += amount;
        order.TotalProfit += profit;

        await _unitOfWork.SaveChangesAsync();
        return newOrderItem;
    }

    public async Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int userId)
    {
        var item = await _orderItemRepository.GetOrderItemWithDetailsAsync(orderItemId);

        if (item?.Variant?.Product == null || item.Order == null)
            throw new KeyNotFoundException("Order item, variant, product, or order not found.");

        if (itemDto.RowVersion != null)
            _orderItemRepository.SetOrderItemRowVersion(item, Convert.FromBase64String(itemDto.RowVersion));

        var oldAmount = item.Amount;
        var oldProfit = item.Profit;
        var quantityChange = 0;

        if (itemDto.Quantity.HasValue)
        {
            quantityChange = itemDto.Quantity.Value - item.Quantity;
            if (quantityChange != 0)
            {
                await _inventoryService.LogTransactionAsync(item.VariantId, "OrderItemUpdate", -quantityChange, item.Id, userId, $"Quantity updated in order {item.OrderId}");
            }
            item.Quantity = itemDto.Quantity.Value;
        }

        if (itemDto.SellingPrice.HasValue)
        {
            if (itemDto.SellingPrice.Value < item.Variant.PurchasePrice)
                throw new ArgumentException("Selling price cannot be less than purchase price.");
            item.SellingPrice = itemDto.SellingPrice.Value;
        }

        var newAmount = item.SellingPrice * item.Quantity;
        var newProfit = (item.SellingPrice - item.Variant.PurchasePrice) * item.Quantity;

        item.Order.TotalAmount = item.Order.TotalAmount - oldAmount + newAmount;
        item.Order.TotalProfit = item.Order.TotalProfit - oldProfit + newProfit;

        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteOrderItemAsync(int orderItemId, int userId)
    {
        var item = await _orderItemRepository.GetOrderItemWithDetailsAsync(orderItemId);
        if (item?.Order == null) throw new KeyNotFoundException("Order item or order not found.");

        await _inventoryService.LogTransactionAsync(item.VariantId, "Return", item.Quantity, item.Id, userId, $"Item removed from order {item.OrderId}");

        item.Order.TotalAmount -= item.Amount;
        item.Order.TotalProfit -= item.Profit;

        _orderItemRepository.DeleteOrderItem(item);

        await _unitOfWork.SaveChangesAsync();
        return true;
    }
}