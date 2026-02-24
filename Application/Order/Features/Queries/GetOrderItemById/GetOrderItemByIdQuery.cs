namespace Application.Order.Features.Queries.GetOrderItemById;

public record GetOrderItemByIdQuery(int Id) : IRequest<ServiceResult<OrderItemDto>>;