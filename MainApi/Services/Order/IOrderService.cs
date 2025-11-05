namespace MainApi.Services.Order;

public interface IOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<TOrders> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey);
    Task<TOrders> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderAsync(int orderId);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<bool> VerifyPaymentAsync(int orderId, string authority);
    string GetFrontendUrl();
}