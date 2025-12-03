namespace Application.Common.Interfaces.Admin;

public interface IAdminOrderStatusService
{
    Task<OrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
}