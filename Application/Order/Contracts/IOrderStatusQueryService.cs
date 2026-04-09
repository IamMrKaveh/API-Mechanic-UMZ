using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;

namespace Application.Order.Contracts;

public interface IOrderStatusQueryService
{
    Task<IReadOnlyList<OrderStatusDto>> GetAllAsync(CancellationToken ct = default);

    Task<OrderStatusDto?> GetByIdAsync(
        OrderStatusId orderStatusId,
        CancellationToken ct = default);
}