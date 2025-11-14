namespace Application.Common.Interfaces;

public interface IOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<Domain.Order.Order> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey);
    Task<(Domain.Order.Order Order, string? PaymentUrl, string? Error)> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusByIdDto statusDto);
    Task<bool> DeleteOrderAsync(int orderId);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<(bool IsVerified, string RedirectUrl)> VerifyAndProcessPaymentAsync(int orderId, string authority, string status);
}