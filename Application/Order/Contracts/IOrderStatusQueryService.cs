using Application.Order.Features.Shared;

namespace Application.Order.Contracts;

public interface IOrderStatusQueryService
{
    Task<IReadOnlyList<OrderStatusDto>> GetAllAsync(
        bool? onlyActive = null,
        CancellationToken ct = default);

    Task<OrderStatusDto?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default);
}