namespace Application.Order.Contracts;

public interface IOrderRepository
{
    Task<bool> ExistsByIdempotencyKeyAsync(string key, CancellationToken ct = default);

    Task AddAsync(Domain.Order.Aggregates.Order order, CancellationToken ct = default);

    Task UpdateAsync(Domain.Order.Aggregates.Order order, CancellationToken ct = default);

    void SetOriginalRowVersion(Domain.Order.Aggregates.Order entity, byte[] rowVersion);

    Task<bool> HasActiveOrdersAsync(int userId, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByIdAsync(int id, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByIdWithItemsAsync(int id, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByIdempotencyKeyAsync(string key, int userId, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByIdempotencyKeyAsync(string key, CancellationToken ct = default);

    Task<IEnumerable<Domain.Order.Aggregates.Order>> GetExpiredUnpaidOrdersAsync(DateTime cutoffTime, int maxCount, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken ct = default);

    Task<Domain.Order.Aggregates.Order?> GetByOrderItemIdAsync(int orderItemId, CancellationToken ct = default);

    Task<IEnumerable<Domain.Order.Aggregates.Order>> GetExpirableOrdersAsync(DateTime expiryThreshold, IEnumerable<string> statuses, CancellationToken ct);
}