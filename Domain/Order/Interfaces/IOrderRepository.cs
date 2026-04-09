using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Domain.Order.Interfaces;

public interface IOrderRepository
{
    Task<Aggregates.Order?> FindByIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<Aggregates.Order?> FindByOrderNumberAsync(
        OrderNumber orderNumber,
        CancellationToken ct = default);

    Task<bool> ExistsByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken ct = default);

    Task<IReadOnlyList<Aggregates.Order>> FindByUserIdAsync(
        UserId userId,
        CancellationToken ct = default);

    void Add(Aggregates.Order order);

    void Update(Aggregates.Order order);
}