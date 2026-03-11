namespace Application.Order.Contracts;

public interface IOrderStatusQueryService
{
    Task<IEnumerable<OrderStatusDto>> GetAllAsync(CancellationToken ct = default);

    Task<OrderStatusDto?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<OrderStatusDto?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<OrderStatusDto?> GetDefaultStatusAsync(CancellationToken ct = default);

    Task<IEnumerable<OrderStatusDto>> GetAllActiveAsync(CancellationToken ct = default);
}