namespace MainApi.Services.Order;

public interface IOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<TOrders> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey);
    Task<TOrders> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto);
    Task<bool> UpdateOrderStatusAsync(int orderId, int statusId);
    Task<bool> DeleteOrderAsync(int orderId);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<IEnumerable<object>> GetOrderItemsAsync(int? currentUserId, bool isAdmin, int? orderId);
    Task<object?> GetOrderItemByIdAsync(int orderItemId, int? currentUserId, bool isAdmin);
    Task<TOrderItems> CreateOrderItemAsync(CreateOrderItemDto itemDto);
    Task<bool> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto);
    Task<bool> DeleteOrderItemAsync(int orderItemId);
    Task<IEnumerable<object>> GetOrderStatusesAsync();
    Task<object?> GetOrderStatusByIdAsync(int id);
    Task<TOrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
    Task<bool> VerifyPaymentAsync(int orderId, string authority);
    string GetFrontendUrl();
}