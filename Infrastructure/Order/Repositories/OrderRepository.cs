using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Infrastructure.Order.Repositories;

public sealed class OrderRepository(DBContext context) : IOrderRepository
{
    public async Task<Domain.Order.Aggregates.Order?> FindByIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<Domain.Order.Aggregates.Order?> FindByOrderNumberAsync(
        OrderNumber orderNumber,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken ct = default)
    {
        return await context.Orders
            .AnyAsync(o => o.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task<IReadOnlyList<Domain.Order.Aggregates.Order>> FindByUserIdAsync(
        UserId userId,
        CancellationToken ct = default)
    {
        var results = await context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId.Value == userId.Value)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<IReadOnlyList<Domain.Order.Aggregates.Order>> FindPendingExpiredAsync(
        CancellationToken ct = default)
    {
        var expiredBefore = DateTime.UtcNow.AddMinutes(-30);
        var expirableStatuses = new[] { "Created", "Reserved", "Pending" };

        var results = await context.Orders
            .Where(o => expirableStatuses.Contains(o.Status.Value)
                        && o.CreatedAt < expiredBefore)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<Domain.Order.Aggregates.Order?> FindWithItemsByIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<Domain.Order.Aggregates.Order?> FindByOrderItemIdAsync(
        OrderItemId orderItemId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(
                o => o.OrderItems.Any(i => i.Id == orderItemId), ct);
    }

    public void Add(Domain.Order.Aggregates.Order order)
    {
        context.Orders.Add(order);
    }

    public void Update(Domain.Order.Aggregates.Order order)
    {
        context.Orders.Update(order);
    }

    public void SetOriginalRowVersion(Domain.Order.Aggregates.Order entity, byte[] rowVersion)
    {
        context.Entry(entity).Property("RowVersion").OriginalValue = rowVersion;
    }
}