using Application.DTOs.Cart;
using Application.DTOs.Order;
using Application.DTOs.Payment;

namespace Application.Common.Interfaces.Order;

public interface IOrderService
{
    Task<(IEnumerable<OrderDto> Orders, int TotalItems)> GetOrdersAsync(int? currentUserId, bool isAdmin, int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<OrderDto?> GetOrderByIdAsync(int orderId, int? currentUserId, bool isAdmin);
    Task<CheckoutFromCartResultDto> CheckoutFromCartAsync(CreateOrderFromCartDto orderDto, int userId, string idempotencyKey);
    Task<PaymentVerificationResultDto> VerifyAndProcessPaymentAsync(int orderId, string authority, string status);
    Task<ServiceResult> CancelOrderAsync(int orderId, int userId);
}