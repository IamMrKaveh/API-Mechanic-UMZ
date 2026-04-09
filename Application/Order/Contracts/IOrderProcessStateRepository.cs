using Domain.Order.ValueObjects;

namespace Application.Order.Contracts;

public interface IOrderProcessStateRepository
{
    Task<OrderProcessState?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task SaveAsync(
        OrderProcessState state,
        CancellationToken ct = default);

    Task UpdateAsync(
        OrderProcessState state,
        CancellationToken ct = default);
}