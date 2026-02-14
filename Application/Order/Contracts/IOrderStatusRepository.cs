namespace Application.Order.Contracts;

public interface IOrderStatusRepository
{
    Task<IEnumerable<OrderStatus>> GetAllAsync(CancellationToken ct = default);

    Task<OrderStatus?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<OrderStatus?> GetByNameAsync(string name, CancellationToken ct = default);

    Task<OrderStatus?> GetDefaultStatusAsync(CancellationToken ct = default);

    Task AddAsync(OrderStatus status, CancellationToken ct = default);

    void Update(OrderStatus status);

    Task<bool> IsInUseAsync(int id, CancellationToken ct = default);

    Task<IEnumerable<OrderStatus>> GetAllActiveAsync(CancellationToken ct = default);

    Task<OrderStatus?> GetDefaultAsync(CancellationToken ct = default);
}