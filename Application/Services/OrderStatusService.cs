namespace Application.Services;

public class OrderStatusService : IOrderStatusService
{
    private readonly IOrderStatusRepository _orderStatusRepository;

    public OrderStatusService(IOrderStatusRepository orderStatusRepository)
    {
        _orderStatusRepository = orderStatusRepository;
    }

    public async Task<IEnumerable<Domain.Order.OrderStatus>> GetOrderStatusesAsync()
    {
        return await _orderStatusRepository.GetOrderStatusesAsync();
    }

    public async Task<Domain.Order.OrderStatus?> GetOrderStatusByIdAsync(int id)
    {
        return await _orderStatusRepository.GetOrderStatusByIdAsync(id);
    }
}