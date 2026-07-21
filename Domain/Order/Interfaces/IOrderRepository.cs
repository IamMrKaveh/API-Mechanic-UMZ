using Domain.Order.ValueObjects;

namespace Domain.Order.Interfaces;

public interface IOrderRepository
{
    Task<Aggregates.Order?> FindByIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<bool> ExistsByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Order>> FindPendingExpiredAsync(
        CancellationToken ct = default);

    Task<Aggregates.Order?> FindByOrderItemIdAsync(
        OrderItemId orderItemId,
        CancellationToken ct = default);

    void Add(Aggregates.Order order);

    void Update(Aggregates.Order order, byte[]? rowVersion = null);

    void SetOriginalRowVersion(
        Aggregates.Order entity,
        byte[] rowVersion);
}
