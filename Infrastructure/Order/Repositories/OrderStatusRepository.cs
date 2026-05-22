using Domain.Order.Entities;
using Domain.Order.Interfaces;
using Domain.Order.ValueObjects;

namespace Infrastructure.Order.Repositories;

public sealed class OrderStatusRepository(DBContext context) : IOrderStatusRepository
{
    public async Task<OrderStatus?> GetByIdAsync(
        OrderStatusId id,
        CancellationToken ct = default)
    {
        return await context.OrderStatuses
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<bool> IsInUseAsync(
        OrderStatusId id,
        CancellationToken ct = default)
    {
        var status = await context.OrderStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, ct);

        if (status is null) return false;

        return await context.Orders
            .AnyAsync(o => o.Status.Value == status.Name, ct);
    }

    public async Task AddAsync(
        OrderStatus orderStatus,
        CancellationToken ct = default)
    {
        await context.OrderStatuses.AddAsync(orderStatus, ct);
    }

    public void Update(OrderStatus orderStatus)
    {
        context.OrderStatuses.Update(orderStatus);
    }

    public void Remove(OrderStatus orderStatus)
    {
        context.OrderStatuses.Remove(orderStatus);
    }
}