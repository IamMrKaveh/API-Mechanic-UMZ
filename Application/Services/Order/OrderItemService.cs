using Application.Common.Interfaces.Inventory;
using Application.Common.Interfaces.Log;
using Application.Common.Interfaces.Order;
using Application.DTOs.Order;

namespace Application.Services.Order;

public class OrderItemService : IOrderItemService
{
    private readonly IOrderItemRepository _orderItemRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IAuditService _auditService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<OrderItemService> _logger;

    public OrderItemService(
        IOrderItemRepository orderItemRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IAuditService auditService,
        IUnitOfWork unitOfWork,
        ILogger<OrderItemService> logger)
    {
        _orderItemRepository = orderItemRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _auditService = auditService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<ServiceResult<OrderItem>> CreateOrderItemAsync(CreateOrderItemDto itemDto, int creatingUserId)
    {
        var order = await _orderRepository.GetOrderForUpdateAsync(itemDto.OrderId);
        if (order == null)
            return ServiceResult<OrderItem>.Fail("Order not found.");

        var variant = await _productRepository.GetVariantByIdForUpdateAsync(itemDto.VariantId);
        if (variant == null)
            return ServiceResult<OrderItem>.Fail("Product variant not found.");

        if (!variant.IsUnlimited && variant.Stock < itemDto.Quantity)
            return ServiceResult<OrderItem>.Fail($"Not enough stock for product {variant.Product.Name}. Available: {variant.Stock}, Requested: {itemDto.Quantity}.");

        var orderItem = new OrderItem
        {
            OrderId = itemDto.OrderId,
            VariantId = itemDto.VariantId,
            Quantity = itemDto.Quantity,
            SellingPrice = itemDto.SellingPrice,
            PurchasePrice = variant.PurchasePrice,
            Amount = itemDto.Quantity * itemDto.SellingPrice,
            Profit = itemDto.Quantity * (itemDto.SellingPrice - variant.PurchasePrice)
        };

        await _orderItemRepository.AddOrderItemAsync(orderItem);
        await _unitOfWork.SaveChangesAsync();

        try
        {
            await _inventoryService.LogTransactionAsync(
                variant.Id,
                "Sale",
                -orderItem.Quantity,
                orderItem.Id,
                creatingUserId,
                $"Added to order {order.Id}",
                $"ORDER-{order.Id}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log inventory transaction for new order item {OrderItemId}. Rolling back.", orderItem.Id);
            _orderItemRepository.RemoveOrderItem(orderItem);
            await _unitOfWork.SaveChangesAsync();
            return ServiceResult<OrderItem>.Fail("Failed to update inventory. Order item creation was rolled back.");
        }

        order.RecalculateTotals();
        await _unitOfWork.SaveChangesAsync();

        await _auditService.LogOrderEventAsync(order.Id, "AddItem", creatingUserId, $"Item '{variant.Product.Name}' (Qty: {orderItem.Quantity}) added.");
        return ServiceResult<OrderItem>.Ok(orderItem);
    }

    public async Task<ServiceResult<bool>> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int updatingUserId)
    {
        var orderItem = await _orderItemRepository.GetOrderItemByIdForUpdateAsync(orderItemId);
        if (orderItem == null)
            return ServiceResult<bool>.Fail("Order item not found.");

        _orderItemRepository.SetOrderItemRowVersion(orderItem, Convert.FromBase64String(itemDto.RowVersion));

        var originalQuantity = orderItem.Quantity;
        var quantityChange = (itemDto.Quantity ?? originalQuantity) - originalQuantity;

        if (quantityChange != 0)
        {
            if (!orderItem.Variant.IsUnlimited && orderItem.Variant.Stock < quantityChange)
                return ServiceResult<bool>.Fail($"Not enough stock to update quantity. Available: {orderItem.Variant.Stock}, Additional required: {quantityChange}.");

            orderItem.Quantity = itemDto.Quantity ?? orderItem.Quantity;
        }

        if (itemDto.SellingPrice.HasValue)
        {
            orderItem.SellingPrice = itemDto.SellingPrice.Value;
        }

        orderItem.RecalculateTotals();

        try
        {
            if (quantityChange != 0)
            {
                await _inventoryService.LogTransactionAsync(
                    orderItem.VariantId,
                    "SaleUpdate",
                    -quantityChange,
                    orderItem.Id,
                    updatingUserId,
                    $"Quantity updated for order {orderItem.OrderId}",
                    $"ORDER-{orderItem.OrderId}"
                );
            }

            var order = await _orderRepository.GetOrderForUpdateAsync(orderItem.OrderId);
            order!.RecalculateTotals();

            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogOrderEventAsync(orderItem.OrderId, "UpdateItem", updatingUserId, $"Item ID {orderItem.Id} updated.");
            return ServiceResult<bool>.Ok(true);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ServiceResult<bool>.Fail("This record was modified by another user. Please refresh and try again.", 409);
        }
        catch (InvalidOperationException ex)
        {
            return ServiceResult<bool>.Fail(ex.Message);
        }
    }

    public async Task<ServiceResult<bool>> DeleteOrderItemAsync(int orderItemId, int deletingUserId)
    {
        var orderItem = await _orderItemRepository.GetOrderItemByIdForUpdateAsync(orderItemId);
        if (orderItem == null)
            return ServiceResult<bool>.Fail("Order item not found.");

        _orderItemRepository.RemoveOrderItem(orderItem);

        try
        {
            await _inventoryService.LogTransactionAsync(
               orderItem.VariantId,
               "Return",
               orderItem.Quantity,
               orderItem.Id,
               deletingUserId,
               $"Item removed from order {orderItem.OrderId}",
               $"ORDER-{orderItem.OrderId}"
           );

            var order = await _orderRepository.GetOrderForUpdateAsync(orderItem.OrderId);
            order!.RecalculateTotals();

            await _unitOfWork.SaveChangesAsync();
            await _auditService.LogOrderEventAsync(orderItem.OrderId, "DeleteItem", deletingUserId, $"Item ID {orderItem.Id} removed from order.");
            return ServiceResult<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete order item {OrderItemId} and adjust inventory.", orderItemId);
            return ServiceResult<bool>.Fail("An error occurred while deleting the item.");
        }
    }

    public async Task<ServiceResult<List<OrderItem>>> GetOrderItemsByOrderIdAsync(int orderId)
    {
        var items = await _orderItemRepository.GetOrderItemsByOrderIdAsync(orderId);
        return ServiceResult<List<OrderItem>>.Ok(items);
    }

    public async Task<ServiceResult<OrderItem?>> GetOrderItemByIdAsync(int orderItemId)
    {
        var item = await _orderItemRepository.GetOrderItemByIdAsync(orderItemId);
        if (item == null)
            return ServiceResult<OrderItem?>.Fail("Order item not found.");
        return ServiceResult<OrderItem?>.Ok(item);
    }
}