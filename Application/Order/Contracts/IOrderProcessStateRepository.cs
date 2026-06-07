using Application.Order.Sagas.State;
using Domain.Order.ValueObjects;

namespace Application.Order.Contracts;

public interface IOrderProcessStateRepository
{
    Task<OrderProcessState?> GetByOrderIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task AddAsync(
        OrderProcessState state,
        CancellationToken ct = default);
}