using Application.Order.Contracts;
using Domain.Order.Enums;
using Domain.Order.ValueObjects;
using Infrastructure.Persistence.Context;

namespace Infrastructure.Order.Repositories;

public sealed class OrderProcessStateRepository(DBContext context) : IOrderProcessStateRepository
{
    public async Task<OrderProcessState?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default)
    {
        return await context.OrderProcessStates
            .FirstOrDefaultAsync(s => s.OrderId == orderId, ct);
    }

    public async Task AddAsync(OrderProcessState state, CancellationToken ct = default)
    {
        await context.OrderProcessStates.AddAsync(state, ct);
    }

    public Task UpdateAsync(OrderProcessState state, CancellationToken ct = default)
    {
        context.OrderProcessStates.Update(state);
        return Task.CompletedTask;
    }
}