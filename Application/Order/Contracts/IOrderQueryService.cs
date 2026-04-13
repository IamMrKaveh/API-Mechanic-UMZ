using Application.Order.Features.Shared;
using Domain.Order.ValueObjects;
using Domain.User.ValueObjects;

namespace Application.Order.Contracts;

public interface IOrderQueryService
{
    Task<OrderDto?> GetOrderByIdAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<OrderDto?> GetOrderByNumberAsync(
        OrderNumber orderNumber,
        CancellationToken ct = default);

    Task<PaginatedResult<OrderListItemDto>> GetUserOrdersAsync(
        UserId userId,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<PaginatedResult<AdminOrderDto>> GetAdminOrdersAsync(
        UserId? userId,
        string? status,
        DateTime? from,
        DateTime? to,
        bool? isPaid,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<AdminOrderDto?> GetAdminOrderDetailsAsync(
        OrderId orderId,
        CancellationToken ct = default);

    Task<OrderDto?> GetOrderDetailsAsync(
        OrderId orderId,
        UserId userId,
        CancellationToken ct = default);

    Task<OrderStatisticsDto> GetOrderStatisticsAsync(
        CancellationToken ct = default);
}