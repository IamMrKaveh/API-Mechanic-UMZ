using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetUserOrders;

public record GetUserOrdersQuery(
    Guid UserId,
    string? Status) : IRequest<ServiceResult<PaginatedResult<OrderDto>>>;