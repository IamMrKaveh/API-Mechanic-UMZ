namespace Application.Common.Interfaces;

public interface IOrderStatusService
{
    Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync();
    Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<Domain.Order.OrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
}