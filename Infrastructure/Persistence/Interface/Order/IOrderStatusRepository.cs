namespace Infrastructure.Persistence.Interface.Order;

public interface IOrderStatusRepository
{
    Task<IEnumerable<OrderStatus>> GetOrderStatusesAsync();
    Task<OrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<OrderStatus?> GetByIdAsync(int id);
    Task<OrderStatus?> GetStatusByNameAsync(string name);
    Task<OrderStatus?> GetOrderStatusByIdForUpdateAsync(int id);
    Task AddOrderStatusAsync(OrderStatus status);
    void UpdateOrderStatus(OrderStatus status);
    Task<bool> IsOrderStatusInUseAsync(int id);
    void DeleteOrderStatus(OrderStatus status);
}