namespace Application.Common.Interfaces;

public interface IOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<(bool IsVerified, string RedirectUrl)> VerifyAndProcessPaymentAsync(int orderId, string authority, string status);
}