namespace Application.Common.Interfaces.Order;

public interface IOrderStatusService
{
    Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync();
    Task<OrderStatus?> GetOrderStatusByIdAsync(int id);
}