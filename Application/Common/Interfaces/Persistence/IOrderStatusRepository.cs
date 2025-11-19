namespace Application.Common.Interfaces.Persistence;

public interface IOrderStatusRepository
{
    Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync();

    Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id);

    Task<Domain.Order.OrderStatus?> GetOrderStatusByIdForUpdateAsync(int id);

    Task AddOrderStatusAsync(Domain.Order.OrderStatus status);

    void UpdateOrderStatus(Domain.Order.OrderStatus status);

    Task<bool> IsOrderStatusInUseAsync(int id);

    void DeleteOrderStatus(Domain.Order.OrderStatus status);
}