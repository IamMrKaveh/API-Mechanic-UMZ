namespace Application.Order.Features.Queries.GetOrderStatusById;

public record GetOrderStatusByIdQuery(int Id) : IRequest<ServiceResult<OrderStatusDto>>;