namespace Application.Common.Interfaces;

public interface IOrderItemService
{
    Task<(IEnumerable<object> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize);

    Task<object?> GetOrderItemByIdAsync(int orderItemId, int? currentUserId, bool isAdmin);

    Task<Domain.Order.OrderItem> CreateOrderItemAsync(CreateOrderItemDto itemDto);

    Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int userId);

    Task<bool> DeleteOrderItemAsync(int orderItemId, int userId);
}