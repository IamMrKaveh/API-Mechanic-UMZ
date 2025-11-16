namespace Application.Common.Interfaces;

public interface IOrderStatusService
{
    Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync();
    Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id);
}