namespace Application.Common.Interfaces;

public interface IOrderItemService
{
    Task<ServiceResult<OrderItem>> CreateOrderItemAsync(CreateOrderItemDto itemDto, int creatingUserId);
    Task<ServiceResult<bool>> UpdateOrderItemAsync(int orderItemId, UpdateOrderItemDto itemDto, int updatingUserId);
    Task<ServiceResult<bool>> DeleteOrderItemAsync(int orderItemId, int deletingUserId);
    Task<ServiceResult<List<OrderItem>>> GetOrderItemsByOrderIdAsync(int orderId);
    Task<ServiceResult<OrderItem?>> GetOrderItemByIdAsync(int orderItemId);
}