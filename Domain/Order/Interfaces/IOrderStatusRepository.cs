using Domain.Order.Entities;
using Domain.Order.ValueObjects;

namespace Domain.Order.Interfaces;

public interface IOrderStatusRepository
{
    Task<OrderStatus?> GetByIdAsync(
        OrderStatusId id,
        CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatus>> GetAllAsync(
        CancellationToken ct = default);

    Task<IReadOnlyList<OrderStatus>> GetActiveStatusesAsync(
        CancellationToken ct = default);

    Task AddAsync(
        OrderStatus orderStatus,
        CancellationToken ct = default);

    void Update(
        OrderStatus orderStatus);

    void Remove(
        OrderStatus orderStatus);
}