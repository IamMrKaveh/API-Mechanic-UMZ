using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetUserOrders;

public record GetUserOrdersQuery(
    Guid UserId,
    string? Status,
    int Page = 1,
    int PageSize = 10) : IRequest<ServiceResult<PaginatedResult<OrderListItemDto>>>;