using Application.DTOs.Order;

namespace Application.Common.Interfaces.Admin.Order;

public interface IAdminOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId);
    Task<ServiceResult> UpdateOrderStatusAsync(int orderId, UpdateOrderStatusByIdDto dto);
    Task<ServiceResult> UpdateOrderAsync(int orderId, UpdateOrderDto dto);
    Task<ServiceResult> DeleteOrderAsync(int orderId);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
    Task<Order> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey);
}