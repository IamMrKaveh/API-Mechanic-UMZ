using Application.Order.Features.Shared;
using SharedKernel.Models;

namespace Application.Order.Contracts;

public interface IOrderQueryService
{
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken ct = default);

    Task<OrderDto?> GetOrderByNumberAsync(string orderNumber, CancellationToken ct = default);

    Task<PaginatedResult<OrderListItemDto>> GetUserOrdersAsync(
        Guid userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<OrderListItemDto>> GetAllOrdersAsync(
        string? status,
        Guid? userId,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<OrderStatisticsDto> GetOrderStatisticsAsync(CancellationToken ct = default);
}