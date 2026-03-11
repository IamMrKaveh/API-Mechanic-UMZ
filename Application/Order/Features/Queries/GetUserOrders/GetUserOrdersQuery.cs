using Application.Common.Models;

namespace Application.Order.Features.Queries.GetUserOrders;

public record GetUserOrdersQuery(int UserId, string? Status, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<OrderDto>>>;