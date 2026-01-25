namespace Application.Common.Interfaces;

public interface IOrderService
{
    Task<(IEnumerable<OrderDto> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<OrderDto?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status);
    Task<ServiceResult<IEnumerable<AvailableShippingMethodDto>>> GetAvailableShippingMethodsForCartAsync(int userId);
}