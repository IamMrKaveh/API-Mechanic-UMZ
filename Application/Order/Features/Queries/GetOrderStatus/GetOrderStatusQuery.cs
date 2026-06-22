using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatus;

public record GetOrderStatusQuery(
    Guid OrderId)
    : IQuery<OrderDto>;