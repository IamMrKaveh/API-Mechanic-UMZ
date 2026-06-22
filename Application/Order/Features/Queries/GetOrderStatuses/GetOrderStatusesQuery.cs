using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatuses;

public record GetOrderStatusesQuery
    : IQuery<IReadOnlyList<OrderStatusDto>>;