using System.Buffers.Binary;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

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

    public async Task<bool> ExistsByIdempotencyKeyAsync(
        Guid idempotencyKey,
        CancellationToken ct = default)
    {
        return await context.Orders
            .AnyAsync(o => o.IdempotencyKey == idempotencyKey, ct);
    }

    public async Task<IReadOnlyList<Domain.Order.Aggregates.Order>> FindPendingExpiredAsync(
        CancellationToken ct = default)
    {
        var expiredBefore = DateTime.UtcNow.AddMinutes(-30);
        var expirableStatuses = new[] { "Created", "Reserved", "Pending" };

        var results = await context.Orders
            .AsNoTracking()
            .Where(o => expirableStatuses.Contains(o.Status.Value)
                        && o.CreatedAt < expiredBefore)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<Domain.Order.Aggregates.Order?> FindByOrderItemIdAsync(
        OrderItemId orderItemId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.OrderItems.Any(i => i.Id == orderItemId))
            .FirstOrDefaultAsync(ct);
    }

    public void Add(Domain.Order.Aggregates.Order order)
    {
        context.Orders.Add(order);
    }

    public void Update(Domain.Order.Aggregates.Order order, byte[]? rowVersion = null)
    {
        context.Orders.Update(order);

        if (rowVersion is not null && rowVersion.Length > 0)
            SetOriginalRowVersion(order, rowVersion);
    }

    public void SetOriginalRowVersion(Domain.Order.Aggregates.Order entity, byte[] rowVersion)
    {
        if (rowVersion is null || rowVersion.Length == 0)
            return;

        var xmin = rowVersion.Length >= 4
            ? BinaryPrimitives.ReadUInt32BigEndian(rowVersion.AsSpan(0, 4))
            : 0u;

        context.Entry(entity).Property("xmin").OriginalValue = xmin;
    }
}
