namespace Domain.Order.Interfaces;

public interface IOrderRepository
{
    Task<Aggregates.Order?> FindByIdAsync(Guid orderId, CancellationToken ct = default);

    Task<Aggregates.Order?> FindByOrderNumberAsync(OrderNumber orderNumber, CancellationToken ct = default);

    Task<bool> ExistsByIdempotencyKeyAsync(Guid idempotencyKey, CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Order>> FindByUserIdAsync(Guid userId, CancellationToken ct = default);

    void Add(Aggregates.Order order);

    void Update(Aggregates.Order order);
}