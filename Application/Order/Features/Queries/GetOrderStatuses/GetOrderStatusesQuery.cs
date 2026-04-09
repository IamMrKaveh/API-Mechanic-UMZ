namespace Application.Order.Features.Queries.GetOrderStatuses;

public record GetOrderStatusesQuery : IRequest<ServiceResult<IEnumerable<OrderStatusDto>>>;