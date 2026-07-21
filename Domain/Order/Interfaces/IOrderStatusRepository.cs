using Domain.Order.Entities;
using Domain.Order.ValueObjects;

namespace Domain.Order.Interfaces;

public interface IOrderStatusRepository
{
    Task<OrderStatus?> GetByIdAsync(OrderStatusId id, CancellationToken ct = default);

    Task<OrderStatus?> GetDefaultAsync(CancellationToken ct = default);

    Task<bool> IsInUseAsync(OrderStatusId id, CancellationToken ct = default);

    Task<bool> ExistsByNameAsync(string name, OrderStatusId? excludeId = null, CancellationToken ct = default);

    Task AddAsync(OrderStatus orderStatus, CancellationToken ct = default);

    void Update(OrderStatus orderStatus, byte[]? rowVersion = null);

    void Remove(OrderStatus orderStatus);

    void SetOriginalRowVersion(OrderStatus entity, byte[] rowVersion);
}
