namespace Infrastructure.Persistence.Interface.Order;

public interface IOrderItemRepository
{
    Task<List<OrderItem>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<OrderItem?> GetOrderItemByIdAsync(int orderItemId);
    Task<OrderItem?> GetOrderItemByIdForUpdateAsync(int orderItemId);
    Task AddOrderItemAsync(OrderItem orderItem);
    void SetOrderItemRowVersion(OrderItem item, byte[] rowVersion);
    void RemoveOrderItem(OrderItem item);
}