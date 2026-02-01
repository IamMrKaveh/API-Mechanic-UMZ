using Application.DTOs.Order;

namespace Application.Common.Interfaces.Admin.Order;

public interface IAdminOrderStatusService
{
    Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync();
    Task<OrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
}