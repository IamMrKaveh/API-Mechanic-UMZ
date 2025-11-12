namespace MainApi.Services.Order.Items;

public interface IOrderItemService
{
    Task<(IEnumerable<object> items, int total)> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId, int page, int pageSize);
    Task<object?> GetOrderItemByIdAsync(int orderItemId, int? currentUserId, bool isAdmin);
    Task<TOrderItems> CreateOrderItemAsync(CreateOrderItemDto itemDto);
    Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int userId);
    Task<bool> DeleteOrderItemAsync(int orderItemId, int userId);
}