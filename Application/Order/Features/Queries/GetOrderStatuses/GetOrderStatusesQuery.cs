using Application.Order.Features.Shared;

namespace Application.Order.Features.Queries.GetOrderStatuses;

public record GetOrderStatusesQuery : IRequest<ServiceResult<PaginatedResult<OrderStatusDto>>>;