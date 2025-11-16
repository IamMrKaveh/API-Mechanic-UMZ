namespace Application.Common.Interfaces;

public interface IAdminOrderService
{
    Task<(IEnumerable<object> Orders, int TotalItems)> GetOrdersAsync(int? userId, int? statusId, DateTime? fromDate, DateTime? toDate, int page, int pageSize);
    Task<object?> GetOrderByIdAsync(int orderId);
    Task<Domain.Order.Order> CreateOrderAsync(CreateOrderDto orderDto, string idempotencyKey);
    Task<bool> UpdateOrderAsync(int orderId, UpdateOrderDto orderDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusByIdDto statusDto);
    Task<bool> DeleteOrderAsync(int orderId);
    Task<object> GetOrderStatisticsAsync(DateTime? fromDate, DateTime? toDate);
}