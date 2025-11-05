namespace MainApi.Services.Order.Status;

public interface IOrderStatusService
{
    Task<IEnumerable<TOrderStatus>> GetOrderStatusesAsync();
    Task<TOrderStatus?> GetOrderStatusByIdAsync(int id);
    Task<TOrderStatus> CreateOrderStatusAsync(CreateOrderStatusDto statusDto);
    Task<bool> UpdateOrderStatusAsync(int id, UpdateOrderStatusDto statusDto);
    Task<bool> DeleteOrderStatusAsync(int id);
}