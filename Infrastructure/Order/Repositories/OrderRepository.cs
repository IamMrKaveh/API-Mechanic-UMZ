using Domain.Order.Aggregates;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Order.Repositories;

public sealed class OrderRepository(DBContext context) : IOrderRepository
{
    public async Task<Domain.Order.Aggregates.Order?> GetByIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
    }

    public async Task<Domain.Order.Aggregates.Order?> GetByOrderNumberAsync(
        OrderNumber orderNumber,
        CancellationToken ct = default)
    {
        return await context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    }

    public async Task<IReadOnlyList<Domain.Order.Aggregates.Order>> GetByUserIdAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var results = await context.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return results.AsReadOnly();
    }

    public async Task<bool> ExistsByIdempotencyKeyAsync(Guid key, CancellationToken ct = default)
    {
        return await context.Orders.AnyAsync(o => o.IdempotencyKey == key, ct);
    }

    public async Task AddAsync(Domain.Order.Aggregates.Order order, CancellationToken ct = default)
    {
        await context.Orders.AddAsync(order, ct);
    }

    public void Update(Domain.Order.Aggregates.Order order)
    {
        context.Orders.Update(order);
    }

    public void SetOriginalRowVersion(Domain.Order.Aggregates.Order order, byte[] rowVersion)
    {
        context.Entry(order).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }
}