using Application.Order.Sagas.State;
using Domain.Order.ValueObjects;

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
}