namespace Application.Common.Interfaces;

public interface IAdminOrderStatusService
{
    Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<Domain.Order.OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
}