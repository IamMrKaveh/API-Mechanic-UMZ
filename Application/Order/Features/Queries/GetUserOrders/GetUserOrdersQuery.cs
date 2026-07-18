using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetUserOrders;

public record GetUserOrdersQuery(
    string? Status,
    int Page = 1,
    int PageSize = 10)
    : IPageQuery<OrderListItemDto>;