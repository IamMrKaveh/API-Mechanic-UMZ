using Application.Common.Results;
using Application.Order.Features.Shared;
using SharedKernel.Models;

namespace Application.Order.Features.Queries.GetUserOrders;

public record GetUserOrdersQuery(Guid UserId, string? Status, int Page, int PageSize) : IRequest<ServiceResult<PaginatedResult<OrderDto>>>;